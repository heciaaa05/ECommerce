"""Nạp danh mục sản phẩm: từ API ASP.NET (mặc định) hoặc từ file JSON (CATALOG_FILE, dùng khi test offline)."""
import json
import os

import httpx


def load_catalog() -> list[dict]:
    catalog_file = os.getenv("CATALOG_FILE")
    if catalog_file:
        with open(catalog_file, encoding="utf-8") as f:
            return json.load(f)

    base_url = os.getenv("API_BASE_URL", "http://localhost:5203").rstrip("/")
    items: list[dict] = []
    page = 1
    with httpx.Client(timeout=15) as client:
        while True:
            resp = client.get(f"{base_url}/api/Products", params={"page": page, "pageSize": 100})
            resp.raise_for_status()
            data = resp.json()
            items.extend(data["items"])
            if not data["items"] or page * data["pageSize"] >= data["totalCount"]:
                break
            page += 1
    return items
