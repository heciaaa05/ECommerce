"""Recommender service (FastAPI). ASP.NET gọi GET /recommend/{productId}?top=5."""
import logging
import os
import threading
import time

from fastapi import FastAPI, HTTPException, Query

from .catalog import load_catalog
from .engine import Recommender, parse_catalog

logger = logging.getLogger("recommender")
logging.basicConfig(level=logging.INFO)

REFRESH_SECONDS = int(os.getenv("REFRESH_SECONDS", "300"))

app = FastAPI(title="ECommerce Recommender", version="1.0")

_lock = threading.Lock()
_engine: Recommender | None = None
_loaded_at = 0.0


def _refresh(force: bool = False) -> Recommender | None:
    """Nạp lại index nếu chưa có hoặc đã cũ; nạp lỗi thì giữ index cũ."""
    global _engine, _loaded_at
    with _lock:
        stale = (time.time() - _loaded_at) > REFRESH_SECONDS
        if force or _engine is None or stale:
            try:
                _engine = Recommender(parse_catalog(load_catalog()))
                _loaded_at = time.time()
                logger.info("Da nap index: %d san pham", len(_engine))
            except Exception as ex:  # API chưa lên, mạng lỗi...
                logger.warning("Khong nap duoc catalog: %s", ex)
                # thử lại sau 15 giây nếu chưa có index nào
                _loaded_at = time.time() - REFRESH_SECONDS + 15
        return _engine


@app.on_event("startup")
def _startup() -> None:
    _refresh(force=True)


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "indexSize": len(_engine) if _engine else 0}


@app.post("/reload")
def reload() -> dict:
    engine = _refresh(force=True)
    return {"indexSize": len(engine) if engine else 0}


@app.get("/recommend/{product_id}")
def recommend(product_id: str, top: int = Query(5, ge=1, le=20)) -> dict:
    engine = _refresh()
    if engine is None:
        raise HTTPException(status_code=503, detail="Catalog chua san sang")
    try:
        items = engine.similar(product_id, top)
    except KeyError:
        # Sản phẩm mới tạo chưa có trong index: thử nạp lại một lần
        engine = _refresh(force=True)
        try:
            items = engine.similar(product_id, top) if engine else []
        except KeyError:
            raise HTTPException(status_code=404, detail="Khong thay san pham trong index")
    return {"productId": product_id, "items": items, "indexSize": len(engine)}
