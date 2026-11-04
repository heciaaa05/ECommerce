"""Gợi ý theo nội dung (content-based): TF-IDF trên tên/thương hiệu/danh mục/mô tả/thuộc tính,
kết hợp độ gần về giá và cùng danh mục.

score = W_TEXT * cosine(TF-IDF) + W_PRICE * price_similarity + W_CATEGORY * same_category
"""
from __future__ import annotations

import json
import math
from dataclasses import dataclass, field

import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import linear_kernel

W_TEXT = 0.70
W_PRICE = 0.15
W_CATEGORY = 0.15
PRICE_RANGE_FACTOR = 4.0  # chênh lệch giá >= 4 lần thì độ gần về giá = 0


@dataclass(frozen=True)
class Item:
    id: str
    name: str
    brand: str
    category: str
    description: str
    price: float
    in_stock: bool
    attributes: dict = field(default_factory=dict)


def parse_catalog(raw: list[dict]) -> list[Item]:
    items = []
    for p in raw:
        attrs: dict[str, set[str]] = {}
        stock = 0
        for v in p.get("variants", []):
            stock += int(v.get("stockQuantity", 0))
            try:
                data = json.loads(v.get("attributesJson") or "{}")
            except json.JSONDecodeError:
                data = {}
            for k, val in data.items():
                attrs.setdefault(str(k), set()).add(str(val))
        items.append(Item(
            id=str(p["id"]),
            name=p.get("name") or "",
            brand=p.get("brand") or "",
            category=p.get("categoryName") or "",
            description=p.get("description") or "",
            price=float(p.get("basePrice") or 0),
            in_stock=stock > 0,
            attributes=attrs,
        ))
    return items


def _document(item: Item) -> str:
    # Lặp thương hiệu/danh mục để chúng có trọng số cao hơn trong TF-IDF
    attr_tokens = []
    for k, values in item.attributes.items():
        for val in values:
            attr_tokens.append(f"{k}_{val}")
            attr_tokens.append(val)
    return " ".join([
        item.name,
        (item.brand + " ") * 3,
        (item.category + " ") * 3,
        item.description,
        " ".join(attr_tokens),
    ])


def _price_similarity(a: float, b: float) -> float:
    if a <= 0 or b <= 0:
        return 0.0
    return max(0.0, 1.0 - abs(math.log(a / b)) / math.log(PRICE_RANGE_FACTOR))


class Recommender:
    def __init__(self, items: list[Item]):
        self.items = items
        self._index = {it.id: i for i, it in enumerate(items)}
        self._text_sim: np.ndarray | None = None
        if len(items) >= 2:
            vec = TfidfVectorizer(sublinear_tf=True, ngram_range=(1, 2), min_df=1)
            matrix = vec.fit_transform([_document(it) for it in items])
            self._text_sim = linear_kernel(matrix)  # TF-IDF đã chuẩn hoá L2 nên đây chính là cosine

    def __len__(self) -> int:
        return len(self.items)

    def similar(self, product_id: str, top_k: int = 5) -> list[dict]:
        if product_id not in self._index:
            raise KeyError(product_id)
        if self._text_sim is None:
            return []

        i = self._index[product_id]
        base = self.items[i]
        results = []
        for j, other in enumerate(self.items):
            if j == i or not other.in_stock:
                continue
            same_cat = base.category == other.category
            score = (
                W_TEXT * float(self._text_sim[i, j])
                + W_PRICE * _price_similarity(base.price, other.price)
                + W_CATEGORY * (1.0 if same_cat else 0.0)
            )
            reasons = []
            if same_cat:
                reasons.append("same_category")
            if base.brand and base.brand == other.brand:
                reasons.append("same_brand")
            if _price_similarity(base.price, other.price) >= 0.7:
                reasons.append("similar_price")
            results.append({"productId": other.id, "score": round(score, 4), "reasons": reasons})

        results.sort(key=lambda r: r["score"], reverse=True)
        return results[: max(1, top_k)]
