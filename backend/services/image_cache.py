import os, threading
from typing import Any, Dict, List, Tuple
from core.config import STATIC_ROOT, WORLDS_DIR, THUMB_DIR, IMAGE_CACHE_FILE, WEBP_DIR
from utils.images import is_image_file, relpath_from_static, build_thumb_image, build_full_image
from utils.fileio import load_json, save_json
import time  # 주기 스케줄러용

_image_cache_lock = threading.Lock()
_image_cache: Dict[str, List[Dict[str, Any]]] = {}  # { world_id: [ {path, width, height, thumb, webp}, ... ] }

# 갱신 중복 방지/상태 관리를 위한 플래그와 락
_refresh_state_lock = threading.Lock()
_is_refreshing = False

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
    current: {wid: {rel_src: (src_abs, filename)}}
    added_images: {wid: {rel_src: (src_abs, filename)}}
    removed_images: {wid: {rel_src, ...}}
    """
    changed = False
    with _image_cache_lock:
        # 1) 삭제된 월드 정리
        for wid in removed_worlds:
            # thumb 디렉터리 정리
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
            # webp 디렉터리 정리
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
            # 캐시 제거
            if wid in _image_cache:
                del _image_cache[wid]
                changed = True

        # 2) 이미지 추가/보정 처리
        for wid, added in added_images.items():
            cached_list = _image_cache.get(wid, [])
            cached_by_path: Dict[str, Dict[str, Any]] = {item["path"]: item for item in cached_list}
            world_changed = False

            for rel_src, (src_abs, src_fname) in added.items():
                thumb_abs, thumb_rel = _thumb_target_path(wid, src_fname)
                webp_abs, webp_rel = _webp_target_path(wid, src_fname)
                cached = cached_by_path.get(rel_src)
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
                    cached_list.append({"path": rel_src, "width": w, "height": h, "thumb": thumb_rel, "webp": webp_rel})
                    world_changed = True
                else:
                    # 썸네일/웹프 누락 보정
                    if _should_regen_thumb(src_abs, thumb_abs):
                        try:
                            w, h = build_thumb_image(src_abs, thumb_abs)
                        except Exception:
                            pass
                        else:
                            if "width" not in cached or "height" not in cached:
                                cached["width"], cached["height"] = w, h
                            world_changed = True
                    if not os.path.exists(webp_abs):
                        try:
                            build_full_image(src_abs, webp_abs)
                        except Exception:
                            pass
                        else:
                            if "webp" not in cached:
                                cached["webp"] = webp_rel
                            world_changed = True
                    if "thumb" not in cached:
                        cached["thumb"] = thumb_rel
                        world_changed = True
                    if "webp" not in cached:
                        cached["webp"] = webp_rel
                        world_changed = True

            if world_changed:
                cached_list.sort(key=lambda x: x["path"])
                _image_cache[wid] = cached_list
                changed = True
            else:
                if wid not in _image_cache:
                    _image_cache[wid] = cached_list

        # 3) 이미지 삭제 처리
        for wid, del_paths in removed_images.items():
            cached_list = _image_cache.get(wid, [])
            if not cached_list:
                continue
            world_changed = False
            to_remove = [item for item in cached_list if item["path"] in del_paths]
            if to_remove:
                for item in to_remove:
                    for rel in (item.get("thumb"), item.get("webp")):
                        if rel:
                            abs_p = os.path.join(STATIC_ROOT, rel.replace("/", os.sep))
                            try:
                                if os.path.exists(abs_p):
                                    os.remove(abs_p)
                            except OSError:
                                pass
                    try:
                        cached_list.remove(item)
                    except ValueError:
                        pass
                world_changed = True
                # 폴더 비었으면 정리
                for d in (os.path.join(THUMB_DIR, wid), os.path.join(WEBP_DIR, wid)):
                    try:
                        if os.path.isdir(d) and not os.listdir(d):
                            os.rmdir(d)
                    except OSError:
                        pass

            if world_changed:
                cached_list.sort(key=lambda x: x["path"])
                _image_cache[wid] = cached_list
                changed = True

        # 4) 현재 존재하는 모든 파일에 대해 누락된 파생물 보정
        # (변경 감지는 하지 않지만, 썸네일/웹프가 없을 경우 생성)
        for wid, files_map in current.items():
            cached_list = _image_cache.get(wid, [])
            cached_by_path: Dict[str, Dict[str, Any]] = {item["path"]: item for item in cached_list}
            world_changed = False
            for rel_src, (src_abs, src_fname) in files_map.items():
                cached = cached_by_path.get(rel_src)
                thumb_abs, thumb_rel = _thumb_target_path(wid, src_fname)
                webp_abs, webp_rel = _webp_target_path(wid, src_fname)
                if cached is None:
                    # 스냅샷에 있는데 캐시에 없다면 신규로 추가
                    try:
                        w, h = build_thumb_image(src_abs, thumb_abs)
                    except Exception:
                        continue
                    if not os.path.exists(webp_abs):
                        try:
                            build_full_image(src_abs, webp_abs)
                        except Exception:
                            pass
                    cached_list.append({"path": rel_src, "width": w, "height": h, "thumb": thumb_rel, "webp": webp_rel})
                    world_changed = True
                else:
                    regen = False
                    if _should_regen_thumb(src_abs, thumb_abs):
                        try:
                            w, h = build_thumb_image(src_abs, thumb_abs)
                        except Exception:
                            pass
                        else:
                            if "width" not in cached or "height" not in cached:
                                cached["width"], cached["height"] = w, h
                            regen = True
                    if not os.path.exists(webp_abs):
                        try:
                            build_full_image(src_abs, webp_abs)
                        except Exception:
                            pass
                        else:
                            if "webp" not in cached:
                                cached["webp"] = webp_rel
                            regen = True
                    if "thumb" not in cached:
                        cached["thumb"] = thumb_rel
                        regen = True
                    if "webp" not in cached:
                        cached["webp"] = webp_rel
                        regen = True
                    if regen:
                        world_changed = True

            if world_changed:
                cached_list.sort(key=lambda x: x["path"])
                _image_cache[wid] = cached_list
                changed = True

    # 5) world 정렬 인덱스 갱신은 worlds_metadata에 위임
    try:
        from services.worlds_metadata import update_sorted_worlds
        update_sorted_worlds(set(current_world_ids), world_ctime)
    except Exception:
        pass

    # 6) 캐시 파일 저장
    if changed:
        payload = [{"world_id": wid, "images": imgs} for wid, imgs in sorted(_image_cache.items())]
        save_json(IMAGE_CACHE_FILE, payload)

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
    with _image_cache_lock:
        return [{"world_id": wid, "images": imgs} for wid, imgs in sorted(_image_cache.items())]

def get_images_by_world_ids(world_ids: List[str]) -> Dict[str, List[Dict[str, Any]]]:
    """
    여러 world_id에 대한 이미지 리스트를 매핑으로 반환.
    """
    with _image_cache_lock:
        return {wid: list(_image_cache.get(wid, [])) for wid in world_ids}