import threading
from typing import Dict, Optional

from core.config import WORLDS_DESCRIPTION_FILE
from utils.fileio import load_json, save_json

# 메모리 구조: { world_id: {"category": str, "description": str} }
_desc_lock = threading.Lock()
_memory: Dict[str, Dict[str, str]] = {}

def load_worlds_description_from_file() -> None:
    data = load_json(WORLDS_DESCRIPTION_FILE, default={})
    if not isinstance(data, dict):
        return
    # 값 정규화
    normalized: Dict[str, Dict[str, str]] = {}
    for wid, item in data.items():
        if not isinstance(wid, str) or not isinstance(item, dict):
            continue
        cat = item.get("category")
        desc = item.get("description")
        normalized[wid] = {
            "category": cat if isinstance(cat, str) else "",
            "description": desc if isinstance(desc, str) else "",
        }
    with _desc_lock:
        _memory.clear()
        _memory.update(normalized)

def _persist() -> None:
    # 저장은 원자적 tmp->replace 로 처리 (utils.fileio.save_json)
    with _desc_lock:
        save_json(WORLDS_DESCRIPTION_FILE, _memory)

def get_world_extra(world_id: str) -> Dict[str, str]:
    with _desc_lock:
        entry = _memory.get(world_id) or {"category": "", "description": ""}
        # 사본 반환
        return {"category": entry.get("category", ""), "description": entry.get("description", "")}

def set_world_category(world_id: str, category: str) -> Dict[str, str]:
    if category is None:
        category = ""
    with _desc_lock:
        entry = _memory.setdefault(world_id, {"category": "", "description": ""})
        entry["category"] = str(category)
        current = {"category": entry["category"], "description": entry.get("description", "")}
    _persist()
    return current

def set_world_description(world_id: str, description: str) -> Dict[str, str]:
    if description is None:
        description = ""
    with _desc_lock:
        entry = _memory.setdefault(world_id, {"category": "", "description": ""})
        entry["description"] = str(description)
        current = {"category": entry.get("category", ""), "description": entry["description"]}
    _persist()
    return current

def get_all_categories() -> list[str]:
    """
    현재 메모리에 저장된 모든 월드의 카테고리 값을 모아
    - 빈 문자열 제외
    - 중복 제거
    - 이름순 정렬
    하여 반환한다.
    """
    with _desc_lock:
        cats = {v.get("category", "").strip() for v in _memory.values()}
    cats.discard("")
    return sorted(cats)