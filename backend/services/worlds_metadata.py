import threading, os
from datetime import datetime, timezone
from typing import Any, Dict, List

import httpx
from core.config import WORLDS_METADATA_CACHE_FILE, WORLDS_METADATA_TTL
from utils.fileio import load_json, save_json

_cache_lock = threading.Lock()
_memory_cache: Dict[str, Dict[str, Any]] = {}  # { world_id: {"payload": Any, "fetched_at": ISO8601} }

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
    - 추가된 월드: 메타데이터 즉시 로드
    - 삭제된 월드: 메모리 캐시에서 제거
    네트워크/API 오류 등은 전체 흐름을 막지 않음.
    """
    try:
        import asyncio
        for wid in added_worlds:
            try:
                # 동기 컨텍스트에서 호출되므로 asyncio.run 사용
                asyncio.run(load_world_metadata(wid))
            except Exception:
                pass
    except Exception:
        pass

    for wid in removed_worlds:
        try:
            remove_world_from_memory_cache(wid)
        except Exception:
            pass


def get_worlds_payloads(world_ids: List[str]) -> List[Dict[str, Any]]:
    """
    주어진 world_ids 순서를 보존하여 payload 리스트를 반환.
    캐시에 없는 id는 건너뜀.
    """
    results: List[Dict[str, Any]] = []
    with _cache_lock:
        for wid in world_ids:
            entry = _memory_cache.get(wid)
            if entry and entry.get("payload") is not None:
                results.append(entry["payload"])
            else:
                results.append(None)
    return results
