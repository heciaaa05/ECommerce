import json
from pathlib import Path

import pytest

from app.engine import Recommender, parse_catalog

CATALOG = json.loads((Path(__file__).parent / "sample_catalog.json").read_text(encoding="utf-8"))
ENGINE = Recommender(parse_catalog(CATALOG))
BY_NAME = {p["name"]: p for p in CATALOG}
BY_ID = {p["id"]: p for p in CATALOG}


def top(name: str, k: int = 5):
    res = ENGINE.similar(BY_NAME[name]["id"], k)
    return [(BY_ID[r["productId"]], r) for r in res]


def test_loaded_all_products():
    assert len(ENGINE) == 32


def test_wireless_fps_mouse_gets_mice_first():
    names = top("Logitech G Pro X Superlight 2", 5)
    assert all(p["categoryName"] == "Chuột gaming" for p, _ in names)


def test_keyboard_recommends_keyboards():
    names = top("Keychron K8 Pro", 4)
    assert sum(p["categoryName"] == "Bàn phím cơ" for p, _ in names) >= 3


def test_monitor_recommends_monitors():
    names = top("LG UltraGear 27GP850-B", 3)
    assert all(p["categoryName"] == "Màn hình gaming" for p, _ in names)


def test_excludes_self_and_sorted_desc():
    pid = BY_NAME["Razer Viper V3 Pro"]["id"]
    res = ENGINE.similar(pid, 10)
    assert pid not in [r["productId"] for r in res]
    scores = [r["score"] for r in res]
    assert scores == sorted(scores, reverse=True)


def test_same_brand_reason_is_reported():
    res = top("Razer Viper V3 Pro", 5)
    assert any("same_brand" in r["reasons"] for _, r in res)


def test_out_of_stock_products_are_skipped():
    raw = json.loads(json.dumps(CATALOG))
    victim = raw[1]
    for v in victim["variants"]:
        v["stockQuantity"] = 0
    engine = Recommender(parse_catalog(raw))
    res = engine.similar(raw[0]["id"], 31)
    assert victim["id"] not in [r["productId"] for r in res]


def test_unknown_product_raises():
    with pytest.raises(KeyError):
        ENGINE.similar("khong-ton-tai")


def test_single_product_catalog_returns_empty():
    engine = Recommender(parse_catalog(CATALOG[:1]))
    assert engine.similar(CATALOG[0]["id"]) == []
