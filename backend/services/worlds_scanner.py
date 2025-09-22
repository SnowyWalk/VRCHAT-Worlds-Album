import os
from typing import Dict, Tuple, Set
from core.config import WORLDS_DIR
from utils.images import is_image_file, relpath_from_static
from utils.fileio import load_json, save_json
from core.config import WORLDS_SNAPSHOT_FILE

# 디스크 스냅샷을 저장: { world_id: set(rel_src) }
_last_snapshot: Dict[str, Set[str]] = {}

# 스캔 실행 상태 관리(중복 실행 방지)
import threading, time
_scan_state_lock = threading.Lock()
_is_scanning = False

# 스냅샷 초기 로드 (프로세스 시작 시)
try:
    _loaded = load_json(WORLDS_SNAPSHOT_FILE, default={})
    if isinstance(_loaded, dict):
        # 파일 포맷: {wid: [rel_src, ...]}
        _last_snapshot = {str(wid): set(paths) for wid, paths in _loaded.items() if isinstance(paths, list)}
except Exception:
    # 손상되었거나 읽기 실패 시 빈 스냅샷으로 시작
    _last_snapshot = {}


def scan_worlds() -> Tuple[Dict[str, Dict[str, Tuple[str, str]]], Dict[str, float]]:
    """
    WORLDS_DIR을 순회하여 현재 디스크 상태 스냅샷과 world ctime을 수집합니다.
    반환:
      - files_current: { world_id: { rel_src: (src_abs, filename) } }
      - world_ctime:   { world_id: ctime(float) }
    """
    files_current: Dict[str, Dict[str, Tuple[str, str]]] = {}
    world_ctime: Dict[str, float] = {}

    if not os.path.isdir(WORLDS_DIR):
        return files_current, world_ctime

    for entry in os.scandir(WORLDS_DIR):
        if not entry.is_dir():
            continue
        world_id = entry.name
        try:
            world_ctime[world_id] = os.path.getctime(entry.path)
        except OSError:
            world_ctime[world_id] = 0.0

        world_map: Dict[str, Tuple[str, str]] = {}
        for root, _, files in os.walk(entry.path):
            for fname in files:
                if not is_image_file(fname):
                    continue
                src_abs = os.path.join(root, fname)
                rel_src = relpath_from_static(src_abs)
                world_map[rel_src] = (src_abs, fname)
        files_current[world_id] = world_map

    return files_current, world_ctime


def _compute_diff(current: Dict[str, Dict[str, Tuple[str, str]]]):
    """
    현재 스냅샷과 _last_snapshot을 비교하여 변경점(월드/이미지 추가/삭제)을 계산합니다.
    반환:
      - added_worlds: Set[str]
      - removed_worlds: Set[str]
      - added_images: Dict[wid, Dict[rel_src, Tuple[src_abs, fname]]]
      - removed_images: Dict[wid, Set[rel_src]]
    """
    global _last_snapshot
    current_sets: Dict[str, Set[str]] = {wid: set(m.keys()) for wid, m in current.items()}
    prev_sets: Dict[str, Set[str]] = _last_snapshot

    current_world_ids = set(current_sets.keys())
    prev_world_ids = set(prev_sets.keys())

    added_worlds = current_world_ids - prev_world_ids
    removed_worlds = prev_world_ids - current_world_ids

    added_images: Dict[str, Dict[str, Tuple[str, str]]] = {}
    removed_images: Dict[str, Set[str]] = {}

    # 월드가 유지된 경우 이미지 증감 계산
    for wid in (current_world_ids & prev_world_ids):
        prev_paths = prev_sets.get(wid, set())
        curr_paths = current_sets.get(wid, set())
        add_paths = curr_paths - prev_paths
        del_paths = prev_paths - curr_paths
        if add_paths:
            added_images[wid] = {rel: current[wid][rel] for rel in add_paths}
        if del_paths:
            removed_images[wid] = del_paths

    # 새로 추가된 월드는 모든 이미지가 "추가"로 간주
    for wid in added_worlds:
        if current.get(wid):
            added_images[wid] = dict(current[wid])

    # 삭제된 월드는 모든 이미지가 "삭제"로 간주
    for wid in removed_worlds:
        prev_paths = prev_sets.get(wid, set())
        if prev_paths:
            removed_images[wid] = set(prev_paths)

    # 스냅샷 갱신 (+ 디스크 저장)
    _last_snapshot = current_sets
    try:
        snapshot_payload = {wid: sorted(list(paths)) for wid, paths in _last_snapshot.items()}
        save_json(WORLDS_SNAPSHOT_FILE, snapshot_payload)
    except Exception:
        # 저장 실패해도 런타임 동작은 계속
        pass

    return added_worlds, removed_worlds, added_images, removed_images


def run_scan_tick() -> None:
    """
    1회의 스캔-디스패치 틱을 수행.
    - 디스크 스캔
    - 정렬 인덱스 선 갱신 (즉시 page 응답 가능)
    - 메타데이터 fetch 비동기 스케줄
    - 이미지 생성/보정 처리
    """
    current, world_ctime = scan_worlds()
    added_worlds, removed_worlds, added_images, removed_images = _compute_diff(current)
    print('added_worlds: ', len(added_worlds), 'removed_worlds: ', len(removed_worlds), 'added_images: ', len(added_images), 'removed_images: ', len(removed_images))

    # 1) (중요) 정렬 인덱스를 즉시 갱신 -> page 요청 즉시 world_id 노출
    try:
        from services.worlds_metadata import update_sorted_worlds
        update_sorted_worlds(set(current.keys()), world_ctime)
    except Exception:
        pass

    # 2) 메타데이터 fetch는 비동기 스케줄만 하고 즉시 반환되도록
    try:
        from services.worlds_metadata import process_worlds_changes
        process_worlds_changes(added_worlds, removed_worlds)
    except Exception:
        pass

    # 아직 fetch되지 않은 world 메타데이터 보충(반드시 성공할 때까지 백그라운드 재시도)
    try:
        from services.worlds_metadata import backfill_unfetched
        backfill_unfetched(set(current.keys()))
    except Exception:
        pass

    # 3) 이미지 생성/보정은 마지막에 (per-world 락과 heavy I/O 비잠금 방식 유지)
    try:
        from services.image_cache import apply_scan_changes
        current_world_ids = set(current.keys())
        apply_scan_changes(
            current=current,
            world_ctime=world_ctime,
            current_world_ids=current_world_ids,
            added_worlds=added_worlds,
            removed_worlds=removed_worlds,
            added_images=added_images,
            removed_images=removed_images,
        )
    except Exception:
        pass


def is_scanning() -> bool:
    with _scan_state_lock:
        return _is_scanning


def _run_scan_worker():
    global _is_scanning
    try:
        run_scan_tick()
    finally:
        with _scan_state_lock:
            _is_scanning = False


def start_scan_async() -> bool:
    """
    스캔 작업을 백그라운드에서 시작.
    이미 실행 중이면 False, 새로 시작했으면 True 반환.
    """
    global _is_scanning
    with _scan_state_lock:
        if _is_scanning:
            return False
        _is_scanning = True
    t = threading.Thread(target=_run_scan_worker, daemon=True)
    t.start()
    return True


def start_periodic_scan(interval_seconds: int = 60) -> None:
    """
    주기 스캔 스케줄러. 이미 실행 중이면 해당 틱은 스킵.
    """

    def _loop():
        while True:
            try:
                start_scan_async()
            except Exception:
                pass
            time.sleep(interval_seconds)

    threading.Thread(target=_loop, daemon=True).start()
