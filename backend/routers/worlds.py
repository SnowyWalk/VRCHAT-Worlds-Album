from fastapi import APIRouter, HTTPException
from httpx import HTTPError
from requests import RequestException

from services.image_cache import get_image_metadata_payload
from services.worlds_metadata import load_world_metadata, get_worlds_payloads, get_worlds_with_images_page

router = APIRouter(prefix="/api")


@router.get("/worlds/images")
async def list_world_images():
    return get_image_metadata_payload()


# @router.get("/worlds/{world_id}/metadata")
# async def get_world_metadata(world_id: str):
#     try:
#         return await load_world_metadata(world_id)
#     except HTTPError as e:
#         status = e.response.status_code if e.response is not None else 502
#         raise HTTPException(status_code=status, detail=str(e))
#     except RequestException as e:
#         raise HTTPException(status_code=502, detail=f"Upstream request failed: {e}")

@router.get("/page/{page_index}")
async def page(page_index: int, page_size: int = 20):
    return get_worlds_with_images_page(page_index, page_size)
