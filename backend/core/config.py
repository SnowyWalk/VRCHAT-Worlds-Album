import os
from datetime import timedelta

STATIC_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "static"))

WORLDS_DIR = os.path.join(STATIC_ROOT, "worlds")
THUMB_DIR = os.path.join(STATIC_ROOT, "thumb")

IMAGE_CACHE_FILE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "image_metadata.json"))
WORLDS_METADATA_CACHE_FILE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "worlds_metadata.json"))

IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff"}

WEBP_QUALITY = 15
WEBP_METHOD = 6
# 거의 원본 품질 WebP 저장 경로/품질
WEBP_DIR = os.path.join(STATIC_ROOT, "view")
ORIGINAL_WEBP_QUALITY = 95

WORLDS_METADATA_TTL = timedelta(hours=24)
# 스캐너 디스크 스냅샷 저장 파일
WORLDS_SNAPSHOT_FILE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "worlds_snapshot.json"))

INDEX_HTML_FILE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "templates", "index.html"))