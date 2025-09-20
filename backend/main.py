import httpx
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
# 메모리 캐시/파일 영속화를 위한 추가 import
import json
import os
import threading
from datetime import datetime, timedelta, timezone
from typing import Any, Dict
import requests
from fastapi import HTTPException

http_client = httpx.AsyncClient(
        timeout=httpx.Timeout(10.0),
        headers={"Accept": "application/json", "User-Agent": "VRCHAT-Worlds-Album/1.0"},
    )

app = FastAPI()

# 정적 파일 라우팅
# /static 경로로 접근하면 static 폴더 내용이 제공됨
app.mount("/static", StaticFiles(directory="static"), name="static")

# API 엔드포인트 예시
@app.get("/api/hello")
async def hello():
    return {"message": "Hello, FastAPI!"}

# 메모리 캐시 + 파일 영속화 설정
CACHE_FILE_PATH = "worlds_metadata.json"
CACHE_TTL = timedelta(hours=24)
_cache_lock = threading.Lock()
_memory_cache: Dict[str, Dict[str, Any]] = {}  # {world_id: {"payload": Any, "fetched_at": iso8601}}

def _load_cache_from_file() -> Dict[str, Dict[str, Any]]:
    if not os.path.exists(CACHE_FILE_PATH):
        return {}
    try:
        with open(CACHE_FILE_PATH, "r", encoding="utf-8") as f:
            data = json.load(f)
            return data if isinstance(data, dict) else {}
    except (json.JSONDecodeError, OSError):
        return {}

def _save_cache_to_file(cache: Dict[str, Dict[str, Any]]) -> None:
    tmp_path = CACHE_FILE_PATH + ".tmp"
    with open(tmp_path, "w", encoding="utf-8") as f:
        json.dump(cache, f, ensure_ascii=False, indent=2)
    os.replace(tmp_path, CACHE_FILE_PATH)

def _is_fresh(entry: Dict[str, Any]) -> bool:
    ts = entry.get("fetched_at")
    if not ts:
        return False
    try:
        fetched_at = datetime.fromisoformat(ts)
        if fetched_at.tzinfo is None:
            fetched_at = fetched_at.replace(tzinfo=timezone.utc)
    except ValueError:
        return False
    return (datetime.now(timezone.utc) - fetched_at) <= CACHE_TTL

async def fetch_world_metadata_from_api(world_id: str) -> Dict[str, Any]:
    global http_client
    api_url = f"https://api.vrchat.cloud/api/1/worlds/{world_id}"
    resp = await http_client.get(api_url, timeout=10)
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
    # 1) 메모리 캐시 확인
    with _cache_lock:
        entry = _memory_cache.get(world_id)
        if entry and entry["payload"] is not None and _is_fresh(entry):
            return entry["payload"]

    # 2) 외부 API에서 최신 데이터 수집
    payload = await fetch_world_metadata_from_api(world_id)
    now_iso = datetime.now(timezone.utc).isoformat()

    # 3) 메모리 캐시 및 파일 동기화
    with _cache_lock:
        _memory_cache[world_id] = {"payload": payload, "fetched_at": now_iso}
        _save_cache_to_file(_memory_cache)

    return payload

@app.get("/api/worlds/{world_id}/metadata")
async def get_world_metadata(world_id: str):
    try:
        data = await load_world_metadata(world_id)
        return data
    except requests.HTTPError as e:
        status = e.response.status_code if e.response is not None else 502
        raise HTTPException(status_code=status, detail=str(e))
    except requests.RequestException as e:
        raise HTTPException(status_code=502, detail=f"Upstream request failed: {e}")



# 서버 시작 시 파일 캐시를 메모리로 미리 로드
with _cache_lock:
    _memory_cache = _load_cache_from_file()
