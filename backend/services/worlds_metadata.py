import threading, os
from datetime import datetime, timezone
from typing import Any, Dict, List
from typing import Set
import time, random

import httpx
from core.config import WORLDS_METADATA_CACHE_FILE, WORLDS_METADATA_TTL
from utils.fileio import load_json, save_json
from concurrent.futures import ThreadPoolExecutor  # 추가: 비동기 스케줄링용
import asyncio

_cache_lock = threading.Lock()
_memory_cache: Dict[str, Dict[str, Any]] = {}  # { world_id: {"payload": Any, "fetched_at": ISO8601} }
_worlds_sorted: List[str] = []  # 생성시간(ctime) 내림차순 정렬된 world_id 목록

# fetch 백그라운드 실행용 풀 (적정 병렬 수는 필요에 따라 조정)
_fetch_executor = ThreadPoolExecutor(max_workers=4)


http_client = httpx.AsyncClient(
    timeout=httpx.Timeout(10.0),
    headers={"Accept": "application/json", "User-Agent": "VRCHAT-Worlds-Album/1.0"},
)


def load_worlds_cache_from_file() -> None:
    data = load_json(WORLDS_METADATA_CACHE_FILE, default={})
    if isinstance(data, dict):
        with _cache_lock:
            _memory_cache.clear()
            _memory_cache.update(data)


def _is_fresh(ts: str) -> bool:
    try:
        dt = datetime.fromisoformat(ts)
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=timezone.utc)
    except Exception:
        return False
    return (datetime.now(timezone.utc) - dt) <= WORLDS_METADATA_TTL


async def fetch_world_metadata_from_api(world_id: str) -> Dict[str, Any]:
    global http_client
    api_url = f"https://api.vrchat.cloud/api/1/worlds/{world_id}"
    resp = await http_client.get(api_url)
    resp.raise_for_status()
    body = resp.json()
    return {
        "id": body["id"],
        "name": body["name"],
        "authorId": body["authorId"],
        "authorName": body["authorName"],
        "imageUrl": body["imageUrl"],
        "capacity": body["capacity"],
        "visits": body["visits"],
        "favorites": body["favorites"],
        "heat": body["heat"],
        "popularity": body["popularity"],
        "tags": body["tags"],
    }


async def load_world_metadata(world_id: str) -> Dict[str, Any]:
    with _cache_lock:
        entry = _memory_cache.get(world_id)
        if entry and entry["payload"] is not None and _is_fresh(entry.get("fetched_at", "")):
            return entry["payload"]

    payload = await fetch_world_metadata_from_api(world_id)
    now_iso = datetime.now(timezone.utc).isoformat()

    with _cache_lock:
        _memory_cache[world_id] = {"payload": payload, "fetched_at": now_iso}
        save_json(WORLDS_METADATA_CACHE_FILE, _memory_cache)
    return payload

def _fetch_world_metadata_sync(world_id: str) -> None:
    """
    스레드 풀에서 실행되는 동기 엔트리. 각 작업은 자체 이벤트 루프에서 실행.
    """
    try:
        asyncio.run(load_world_metadata(world_id))
    except Exception:
        # 개별 실패는 전체 흐름에 영향 주지 않음
        pass

def _ensure_world_metadata_sync(world_id: str, initial_delay: float = 1.0, max_delay: float = 60.0) -> None:
    """
    반드시 성공할 때까지 재시도. 지수 백오프 + 지터 적용.
    """
    delay = initial_delay
    while True:
        try:
            # 성공하거나 캐시가 이미 신선하면 반환
            asyncio.run(load_world_metadata(world_id))
            return
        except Exception:
            # 실패 시 대기 후 재시도
            sleep_for = min(delay, max_delay) * (0.8 + random.random() * 0.4)
            try:
                time.sleep(sleep_for)
            except Exception:
                pass
            delay = min(delay * 2.0, max_delay)

def schedule_fetch_worlds_metadata(world_ids: set[str]) -> None:
    """
    추가된 월드들의 메타데이터 fetch를 백그라운드로 스케줄한다.
    즉시 반환한다.
    """
    for wid in world_ids:
        try:
            _fetch_executor.submit(_ensure_world_metadata_sync, wid)
        except Exception:
            pass

def remove_world_from_memory_cache(world_id: str) -> bool:
    with _cache_lock:
        existed = world_id in _memory_cache
        if existed:
            _memory_cache.pop(world_id, None)
            try:
                save_json(WORLDS_METADATA_CACHE_FILE, _memory_cache)
            except Exception:
                pass
        return existed

def process_worlds_changes(added_worlds: set[str], removed_worlds: set[str]) -> None:
    """
    스캐너가 전달한 월드 증감에 따라 메모리 캐시를 동기화.
    우선순위:
      1) 정렬 인덱스 갱신은 스캐너에서 선반영
      2) 메타데이터 fetch는 백그라운드 스케줄
      3) 삭제된 월드는 즉시 제거
    """
    # 추가된 월드: 비동기 스케줄만 등록하고 즉시 반환
    try:
        if added_worlds:
            schedule_fetch_worlds_metadata(added_worlds)
    except Exception:
        pass

    # 삭제된 월드: 즉시 제거
    for wid in removed_worlds:
        try:
            remove_world_from_memory_cache(wid)
        except Exception:
            pass

def backfill_unfetched(current_world_ids: Set[str]) -> None:
    """
    현재 존재하는 world_id 중 아직 payload가 없는 항목을 찾아
    반드시 성공할 때까지 재시도하는 fetch를 백그라운드로 스케줄한다.
    """
    missing: Set[str] = set()
    with _cache_lock:
        for wid in current_world_ids:
            entry = _memory_cache.get(wid)
            if not entry or entry.get("payload") is None:
                missing.add(wid)
    if not missing:
        return
    try:
        for wid in missing:
            _fetch_executor.submit(_ensure_world_metadata_sync, wid)
    except Exception:
        pass

def get_worlds_payloads(world_ids: List[str]) -> List[Dict[str, Any]]:
    """
    주어진 world_ids 순서를 보존하여 payload 리스트를 반환.
    캐시에 아직 없는 id는 빈 dict로 대체하되 id 필드는 채워 둔다.
    """
    results: List[Dict[str, Any]] = []
    with _cache_lock:
        for wid in world_ids:
            entry = _memory_cache.get(wid)
            if entry and entry.get("payload") is not None:
                results.append(entry["payload"])
            else:
                # 메타가 없더라도 id는 포함하여 클라이언트가 로딩 상태를 표현 가능
                results.append({"id": wid})
    return results

def update_sorted_worlds(current_world_ids: set[str], world_ctime: Dict[str, float]) -> None:
    """
    이미지 생성과 무관하게, 스캔 직후 사용할 수 있도록 정렬 인덱스를 빠르게 갱신.
    """
    global _worlds_sorted
    sorted_worlds = sorted(current_world_ids, key=lambda wid: world_ctime.get(wid, 0.0), reverse=True)
    with _cache_lock:
        if sorted_worlds != _worlds_sorted:
            _worlds_sorted = sorted_worlds


def get_world_ids_page(page_index: int, page_size: int = 20) -> List[str]:
    """
    정렬된 월드 id 목록에서 페이지 단위로 잘라 반환.
    """
    if page_index < 0:
        page_index = 0
    with _cache_lock:
        start = page_index * page_size
        end = start + page_size
        return list(_worlds_sorted[start:end])


def get_worlds_with_images_page(page_index: int, page_size: int = 20) -> List[Dict[str, Any]]:
    """
    페이지 단위로 월드 메타데이터와 이미지 캐시 정보를 합쳐서 반환.
    반환 예: [{...월드메타필드..., "images": [...]}, ...]
    """
    ids = get_world_ids_page(page_index, page_size)
    worlds = get_worlds_payloads(ids)
    try:
        from services.image_cache import get_images_by_world_ids
        images_map = get_images_by_world_ids(ids)
    except Exception:
        images_map = {}

    result: List[Dict[str, Any]] = []
    for wid, meta in zip(ids, worlds):
        payload = dict(meta) if isinstance(meta, dict) else {}
        payload["id"] = payload.get("id", wid) or wid
        payload["images"] = images_map.get(wid, [])
        result.append(payload)
    return result
