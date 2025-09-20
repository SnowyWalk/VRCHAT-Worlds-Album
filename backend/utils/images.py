import os
from typing import Tuple
from PIL import Image  # pillow
from core.config import IMAGE_EXTS, STATIC_ROOT, WEBP_METHOD, WEBP_QUALITY, ORIGINAL_WEBP_QUALITY

def is_image_file(filename: str) -> bool:
    _, ext = os.path.splitext(filename.lower())
    return ext in IMAGE_EXTS

def relpath_from_static(abs_path: str) -> str:
    ap = os.path.abspath(abs_path)
    return os.path.relpath(ap, os.path.abspath(STATIC_ROOT)).replace("\\", "/")

def build_thumb_image(src_abs_path: str, dst_abs_path: str) -> Tuple[int, int]:
    with Image.open(src_abs_path) as im:
        if getattr(im, "is_animated", False):
            im.seek(0)
        if im.mode in ("RGBA", "LA", "P"):
            im = im.convert("RGB")
        w, h = im.size
        im.save(dst_abs_path, format="WEBP", quality=WEBP_QUALITY, method=WEBP_METHOD, optimize=True)
        return w, h

def build_full_image(src_abs_path: str, dst_abs_path: str) -> Tuple[int, int]:
    """
    원본과 동일한 픽셀 크기의 '거의 원본 품질' WebP로 저장.
    """
    with Image.open(src_abs_path) as im:
        if getattr(im, "is_animated", False):
            im.seek(0)
        if im.mode in ("RGBA", "LA", "P"):
            im = im.convert("RGB")
        w, h = im.size
        im.save(dst_abs_path, format="WEBP", quality=ORIGINAL_WEBP_QUALITY, method=WEBP_METHOD, optimize=True)
        return w, h