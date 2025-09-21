# Python
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles

from routers.worlds import router as worlds_router
from services.image_cache import load_image_cache_from_file
from services.worlds_metadata import load_worlds_cache_from_file
from core.config import STATIC_ROOT
from services.worlds_scanner import start_periodic_scan, start_scan_async

app = FastAPI()

app.include_router(worlds_router)

app.mount("/static", StaticFiles(directory=STATIC_ROOT), name="static")

@app.on_event("startup")
async def on_startup():
    load_worlds_cache_from_file()
    load_image_cache_from_file()
    start_scan_async()
    start_periodic_scan(interval_seconds=60)
