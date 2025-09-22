import os, threading
from typing import Any, Dict, List, Tuple
from core.config import STATIC_ROOT, WORLDS_DIR, THUMB_DIR, IMAGE_CACHE_FILE, WEBP_DIR
from utils.images import is_image_file, relpath_from_static, build_thumb_image, build_full_image
from utils.fileio import load_json, save_json
import time  # 주기 스케줄러용
from contextlib import contextmanager  # 락 일시 해제용

_image_cache_lock = threading.Lock()
_image_cache: Dict[str, List[Dict[str, Any]]] = {}  # { world_id: [ {path, width, height, thumb, webp}, ... ] }

# per-world 쓰기 보호용 락
_world_locks_lock = threading.Lock()
_world_locks: Dict[str, threading.Lock] = {}

def _get_world_lock(world_id: str) -> threading.Lock:
    with _world_locks_lock:
        lock = _world_locks.get(world_id)
        if lock is None:
            lock = threading.Lock()
            _world_locks[world_id] = lock
        return lock

@contextmanager
def _lock_released(lock: threading.Lock):
    """
    이미 획득한 락을 일시 해제했다가 블록 종료 시 다시 획득한다.
    주의: 이 컨텍스트는 lock을 이미 쥐고 있는 구간 내부에서만 사용해야 함.
    """
    lock.release()
    try:
        yield
    finally:
        lock.acquire()

def _ensure_dir(path: str) -> None:
    os.makedirs(path, exist_ok=True)

def _thumb_target_path(world_id: str, src_filename: str) -> Tuple[str, str]:
    base, _ = os.path.splitext(os.path.basename(src_filename))
    tdir = os.path.join(THUMB_DIR, world_id)
    _ensure_dir(tdir)
    abs_thumb = os.path.join(tdir, base + ".webp")
    rel_thumb = relpath_from_static(abs_thumb)
    return abs_thumb, rel_thumb

def _webp_target_path(world_id: str, src_filename: str) -> Tuple[str, str]:
    base, _ = os.path.splitext(os.path.basename(src_filename))
    wdir = os.path.join(WEBP_DIR, world_id)
    _ensure_dir(wdir)
    abs_webp = os.path.join(wdir, base + ".webp")
    rel_webp = relpath_from_static(abs_webp)
    return abs_webp, rel_webp

def _should_regen_thumb(src_path: str, thumb_path: str) -> bool:
    # 존재 여부만 확인
    return not os.path.exists(thumb_path)

def apply_scan_changes(
    *,
    current: Dict[str, Dict[str, Tuple[str, str]]],
    world_ctime: Dict[str, float],
    current_world_ids: set,
    added_worlds: set,
    removed_worlds: set,
    added_images: Dict[str, Dict[str, Tuple[str, str]]],
    removed_images: Dict[str, set],
) -> None:
    """
    worlds_scanner.run_scan_tick에서 전달한 스냅샷/변경사항을 적용한다.
    현재 구현은 per-world 락으로 세계별 동시성 분리.
    """
    changed_any = False

    # 1) 삭제된 월드 정리: 맵에서 먼저 제거(짧게 전역락), 파일 삭제는 락 없이
    for wid in removed_worlds:
        with _image_cache_lock:
            existed = wid in _image_cache
            if existed:
                del _image_cache[wid]
        if existed:
            changed_any = True
        # 파일/폴더 정리는 heavy -> 락 없이 실행
        thumb_dir = os.path.join(THUMB_DIR, wid)
        if os.path.isdir(thumb_dir):
            try:
                for root, _, files in os.walk(thumb_dir, topdown=False):
                    for f in files:
                        try:
                            os.remove(os.path.join(root, f))
                        except OSError:
                            pass
                    try:
                        os.rmdir(root)
                    except OSError:
                        pass
            except Exception:
                pass
        webp_dir = os.path.join(WEBP_DIR, wid)
        if os.path.isdir(webp_dir):
            try:
                for root, _, files in os.walk(webp_dir, topdown=False):
                    for f in files:
                        try:
                            os.remove(os.path.join(root, f))
                        except OSError:
                            pass
                    try:
                        os.rmdir(root)
                    except OSError:
                        pass
            except Exception:
                pass

    # 2) 이미지 추가/보정 처리: 월드별로 락을 잡고, heavy는 락 없이
    for wid, added in added_images.items():
        wlock = _get_world_lock(wid)

        # 현재 캐시 스냅샷 사본 획득(짧게)
        with wlock:
            cached_list = list(_image_cache.get(wid, []))
            cached_by_path: Dict[str, Dict[str, Any]] = {item["path"]: dict(item) for item in cached_list}

        world_changed = False
        to_append: List[Dict[str, Any]] = []

        for rel_src, (src_abs, src_fname) in added.items():
            thumb_abs, thumb_rel = _thumb_target_path(wid, src_fname)
            webp_abs, webp_rel = _webp_target_path(wid, src_fname)
            cached = cached_by_path.get(rel_src)

            if cached is None:
                # heavy 작업은 락 없이
                try:
                    w, h = build_thumb_image(src_abs, thumb_abs)
                except Exception:
                    continue
                if not os.path.exists(webp_abs):
                    try:
                        build_full_image(src_abs, webp_abs)
                    except Exception:
                        pass
                to_append.append({"path": rel_src, "width": w, "height": h, "thumb": thumb_rel, "webp": webp_rel})
                world_changed = True
            else:
                regen_thumb = _should_regen_thumb(src_abs, thumb_abs)
                regen_webp = not os.path.exists(webp_abs)
                new_w = new_h = None
                if regen_thumb or regen_webp:
                    # heavy
                    if regen_thumb:
                        try:
                            new_w, new_h = build_thumb_image(src_abs, thumb_abs)
                        except Exception:
                            new_w = new_h = None
                    if regen_webp:
                        try:
                            build_full_image(src_abs, webp_abs)
                        except Exception:
                            pass
                # 캐시 보정은 락 안에서
                if regen_thumb and (new_w is not None and new_h is not None):
                    if "width" not in cached or "height" not in cached:
                        cached["width"], cached["height"] = new_w, new_h
                    world_changed = True
                if regen_webp:
                    if "webp" not in cached:
                        cached["webp"] = webp_rel
                    world_changed = True
                if "thumb" not in cached:
                    cached["thumb"] = thumb_rel
                    world_changed = True
                if "webp" not in cached:
                    cached["webp"] = webp_rel
                    world_changed = True

        if world_changed or to_append:
            with wlock:
                # 최신 리스트 로드 후 반영(보수적으로 재적용)
                cur_list = list(_image_cache.get(wid, []))
                cur_by_path = {it["path"]: it for it in cur_list}
                # 기존 수정 반영
                for rel_src, (src_abs, src_fname) in added.items():
                    cached = cached_by_path.get(rel_src)
                    if cached is not None:
                        target = cur_by_path.get(rel_src)
                        if target is None:
                            cur_list.append(cached)
                        else:
                            # 누락 필드 채우기
                            for k in ("width", "height", "thumb", "webp"):
                                if k in cached and k not in target:
                                    target[k] = cached[k]
                # 신규 추가
                cur_list.extend(to_append)
                cur_list.sort(key=lambda x: x["path"])
                _image_cache[wid] = cur_list
            changed_any = True

    # 3) 이미지 삭제 처리: 월드별 락으로 리스트 변경, 파일 삭제는 락 없이
    for wid, del_paths in removed_images.items():
        wlock = _get_world_lock(wid)
        to_remove: List[Dict[str, Any]] = []
        with wlock:
            cached_list = _image_cache.get(wid, [])
            if cached_list:
                to_remove = [item for item in cached_list if item["path"] in del_paths]
                if to_remove:
                    for item in to_remove:
                        try:
                            cached_list.remove(item)
                        except ValueError:
                            pass
                    cached_list.sort(key=lambda x: x["path"])
                    _image_cache[wid] = cached_list
                    changed_any = True
        # 파일 삭제는 락 없이
        for item in to_remove:
            for rel in (item.get("thumb"), item.get("webp")):
                if rel:
                    abs_p = os.path.join(STATIC_ROOT, rel.replace("/", os.sep))
                    try:
                        if os.path.exists(abs_p):
                            os.remove(abs_p)
                    except OSError:
                        pass
        # 폴더 비었으면 정리
        for d in (os.path.join(THUMB_DIR, wid), os.path.join(WEBP_DIR, wid)):
            try:
                if os.path.isdir(d) and not os.listdir(d):
                    os.rmdir(d)
            except OSError:
                pass

    # 4) 현재 존재하는 모든 파일에 대해 누락된 파생물 보정(월드별로 분리)
    for wid, files_map in current.items():
        wlock = _get_world_lock(wid)
        with wlock:
            cached_list = list(_image_cache.get(wid, []))
            cached_by_path: Dict[str, Dict[str, Any]] = {item["path"]: dict(item) for item in cached_list}
        world_changed = False
        to_append: List[Dict[str, Any]] = []

        for rel_src, (src_abs, src_fname) in files_map.items():
            cached = cached_by_path.get(rel_src)
            thumb_abs, thumb_rel = _thumb_target_path(wid, src_fname)
            webp_abs, webp_rel = _webp_target_path(wid, src_fname)
            if cached is None:
                try:
                    w, h = build_thumb_image(src_abs, thumb_abs)
                except Exception:
                    continue
                if not os.path.exists(webp_abs):
                    try:
                        build_full_image(src_abs, webp_abs)
                    except Exception:
                        pass
                to_append.append({"path": rel_src, "width": w, "height": h, "thumb": thumb_rel, "webp": webp_rel})
                world_changed = True
            else:
                regen_thumb = _should_regen_thumb(src_abs, thumb_abs)
                regen_webp = not os.path.exists(webp_abs)
                new_w = new_h = None
                if regen_thumb:
                    try:
                        new_w, new_h = build_thumb_image(src_abs, thumb_abs)
                    except Exception:
                        new_w = new_h = None
                if regen_webp:
                    try:
                        build_full_image(src_abs, webp_abs)
                    except Exception:
                        pass
                if regen_thumb and (new_w is not None and new_h is not None):
                    if "width" not in cached or "height" not in cached:
                        cached["width"], cached["height"] = new_w, new_h
                    world_changed = True
                if regen_webp:
                    if "webp" not in cached:
                        cached["webp"] = webp_rel
                    world_changed = True
                if "thumb" not in cached:
                    cached["thumb"] = thumb_rel
                    world_changed = True
                if "webp" not in cached:
                    cached["webp"] = webp_rel
                    world_changed = True

        if world_changed or to_append:
            with wlock:
                cur_list = list(_image_cache.get(wid, []))
                cur_by_path = {it["path"]: it for it in cur_list}
                for rel_src, info in cached_by_path.items():
                    if rel_src in cur_by_path:
                        target = cur_by_path[rel_src]
                        for k in ("width", "height", "thumb", "webp"):
                            if k in info and k not in target:
                                target[k] = info[k]
                cur_list.extend(to_append)
                cur_list.sort(key=lambda x: x["path"])
                _image_cache[wid] = cur_list
            changed_any = True

    # 5) world 정렬 인덱스 갱신은 worlds_metadata에 위임(락 없음)
    try:
        from services.worlds_metadata import update_sorted_worlds
        update_sorted_worlds(set(current_world_ids), world_ctime)
    except Exception:
        pass

    # 6) 캐시 파일 저장
    if changed_any:
        # 맵 전체를 직렬화해야 하므로 짧게 전역 락 -> 사본 만든 뒤 저장
        with _image_cache_lock:
            snapshot = [{"world_id": wid, "images": imgs} for wid, imgs in sorted(_image_cache.items())]
        save_json(IMAGE_CACHE_FILE, snapshot)

def run_worlds_scan_tick() -> Dict[str, int]:
    """
    폴더 스캔 및 변경사항 적용은 worlds_scanner가 담당.
    여기서는 한 틱 실행을 위임한 뒤, 현재 통계를 반환한다.
    """
    try:
        from services.worlds_scanner import run_scan_tick
        run_scan_tick()
    except Exception:
        pass

    with _image_cache_lock:
        worlds_count = len(_image_cache)
        images_count = sum(len(v) for v in _image_cache.values())
    return {"worlds": worlds_count, "images": images_count}

def load_image_cache_from_file() -> None:
    data = load_json(IMAGE_CACHE_FILE, default=[])
    if not isinstance(data, list):
        return
    mapping: Dict[str, List[Dict[str, Any]]] = {}
    for item in data:
        wid = item.get("world_id")
        imgs = item.get("images", [])
        if isinstance(wid, str) and isinstance(imgs, list):
            mapping[wid] = imgs
    with _image_cache_lock:
        _image_cache.clear()
        _image_cache.update(mapping)
        # 정렬 인덱스는 worlds_metadata에서 갱신됩니다.

def get_image_metadata_payload():
    # 모든 월드를 순회해야 하므로 키 목록만 잠깐 확보
    with _image_cache_lock:
        keys = list(_image_cache.keys())
    result = []
    for wid in sorted(keys):
        wlock = _get_world_lock(wid)
        with wlock:
            images = list(_image_cache.get(wid, []))
        result.append({"world_id": wid, "images": images})
    return result

def get_images_by_world_ids(world_ids: List[str]) -> Dict[str, List[Dict[str, Any]]]:
    """
    여러 world_id에 대한 이미지 리스트를 per-world 락으로 안전하게 반환.
    """
    out: Dict[str, List[Dict[str, Any]]] = {}
    for wid in world_ids:
        wlock = _get_world_lock(wid)
        with wlock:
            out[wid] = list(_image_cache.get(wid, []))
    return out