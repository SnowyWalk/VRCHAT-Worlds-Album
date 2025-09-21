from fastapi import APIRouter, HTTPException
from httpx import HTTPError
from requests import RequestException

from services.image_cache import get_image_metadata_payload
from services.worlds_metadata import load_world_metadata, get_worlds_payloads, get_worlds_with_images_page
from pydantic import BaseModel
from services.worlds_description import set_world_category, set_world_description, get_world_extra, get_all_categories

router = APIRouter(prefix="/api")

class CategoryUpdate(BaseModel):
    category: str

class DescriptionUpdate(BaseModel):
    description: str

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
async def page(page_index: int, page_size: int = 10):
    import time
    return get_worlds_with_images_page(page_index, page_size)

@router.post("/admin/worlds/{world_id}/category")
async def update_world_category(world_id: str, payload: CategoryUpdate):
    data = set_world_category(world_id, payload.category)
    # world_id를 함께 반환
    return {"world_id": world_id, **data}

@router.post("/admin/worlds/{world_id}/description")
async def update_world_description(world_id: str, payload: DescriptionUpdate):
    data = set_world_description(world_id, payload.description)
    return {"world_id": world_id, **data}

@router.get("/get_categories")
async def get_categories():
    return get_all_categories()
