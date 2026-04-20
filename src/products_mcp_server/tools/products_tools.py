"""Mocked product catalog and inventory tools used by the Products MCP server.

These functions simulate a real Products API integration.
In production, replace the mock data with real HTTP calls to a product
catalog service (e.g. a REST API backed by Azure Cosmos DB or SQL Database).
"""

from __future__ import annotations
from dataclasses import dataclass


@dataclass(frozen=True)
class _Product:
    id: int
    name: str
    category: str
    price: float
    stock: int
    tags: tuple[str, ...]
    description: str


_MOCK_PRODUCTS: list[_Product] = [
    _Product(1,  "Alpine Waterproof Jacket",  "Outerwear",   189.99, 42,
             ("waterproof", "jacket", "outdoor", "camping", "hiking"),
             "Lightweight, fully waterproof jacket with sealed seams. 3-layer Gore-Tex® construction."),
    _Product(2,  "Trail Running Shoes",        "Footwear",    129.99, 18,
             ("shoes", "trail", "running", "outdoor"),
             "Aggressive grip outsole with rock plate protection. Waterproof upper."),
    _Product(3,  "4-Season Tent",              "Camping",     449.00,  7,
             ("tent", "camping", "shelter", "outdoor"),
             "Freestanding 3-pole design rated for 4 seasons. Sleeps 2 adults comfortably."),
    _Product(4,  "Sleeping Bag -10°C",         "Camping",     219.00, 15,
             ("sleeping bag", "camping", "outdoor"),
             "Down-filled sleeping bag rated to -10°C / 14°F. Packable to 4L."),
    _Product(5,  "Trekking Poles Set",         "Accessories",  79.99, 30,
             ("poles", "trekking", "hiking", "camping"),
             "Collapsible aluminium poles with cork grips and tungsten carbide tips."),
    _Product(6,  "Headlamp 350 Lumens",        "Accessories",  49.99, 55,
             ("headlamp", "light", "camping", "outdoor"),
             "Rechargeable USB-C headlamp with red light mode. IPX4 water resistant."),
    _Product(7,  "Merino Wool Base Layer",     "Clothing",     89.99, 24,
             ("wool", "base layer", "merino", "clothing"),
             "Natural temperature regulation. Odour resistant. Machine washable."),
    _Product(8,  "Insulated Thermos 1L",       "Accessories",  34.99, 60,
             ("thermos", "bottle", "camping", "outdoor"),
             "Double-wall vacuum insulation. Keeps liquids hot 12 hrs / cold 24 hrs."),
    _Product(9,  "Rain Pants",                 "Outerwear",    79.99, 20,
             ("rain", "pants", "waterproof", "outdoor"),
             "Lightweight and packable waterproof trousers with full-length zips."),
    _Product(10, "Camp Stove Compact",         "Camping",      59.99, 12,
             ("stove", "camping", "cooking", "outdoor"),
             "Compact canister stove. Boils 1L in 3 minutes. 185g packed weight."),
]


def search_products(query: str, max_results: int = 5) -> str:
    """Search for products by name, category, or keyword.

    Args:
        query: Search query – product name, category, or keyword.
        max_results: Maximum number of results to return (default 5).

    Returns:
        A formatted list of matching products, or a descriptive message if
        none are found.
    """
    if not query or not query.strip():
        return "Please provide a search query."

    terms = query.lower().split()
    results = [
        p for p in _MOCK_PRODUCTS
        if any(
            t in p.name.lower()
            or t in p.category.lower()
            or any(t in tag for tag in p.tags)
            for t in terms
        )
    ][:max_results]

    if not results:
        return f"No products found for '{query}'."

    lines = [
        f"  [{p.id}] {p.name}  |  {p.category}  |  ${p.price:.2f}  |  "
        + (f"In stock ({p.stock})" if p.stock > 0 else "Out of stock")
        for p in results
    ]
    return f"Found {len(results)} product(s) matching '{query}':\n" + "\n".join(lines)


def get_product_details(product_id: int) -> str:
    """Get detailed information about a product by its ID.

    Args:
        product_id: The numeric product ID (from search_products results).

    Returns:
        A formatted product detail string, or an error message if not found.
    """
    product = next((p for p in _MOCK_PRODUCTS if p.id == product_id), None)
    if product is None:
        return f"Product with ID {product_id} not found."

    stock_str = (
        f"{product.stock} units available" if product.stock > 0 else "Out of stock"
    )
    return (
        "Product Details:\n"
        f"  ID          : {product.id}\n"
        f"  Name        : {product.name}\n"
        f"  Category    : {product.category}\n"
        f"  Price       : ${product.price:.2f}\n"
        f"  Stock       : {stock_str}\n"
        f"  Description : {product.description}\n"
        f"  Tags        : {', '.join(product.tags)}"
    )


def check_inventory(product_id: int) -> str:
    """Check inventory availability for a product.

    Args:
        product_id: The numeric product ID to check inventory for.

    Returns:
        A human-readable inventory status string.
    """
    product = next((p for p in _MOCK_PRODUCTS if p.id == product_id), None)
    if product is None:
        return f"Product with ID {product_id} not found."

    if product.stock == 0:
        status = "\u274c Out of stock"
    elif product.stock <= 5:
        status = f"\u26a0\ufe0f  Low stock \u2013 only {product.stock} unit(s) remaining"
    elif product.stock <= 15:
        status = f"\u2705 In stock \u2013 {product.stock} units available"
    else:
        status = f"\u2705 Well stocked \u2013 {product.stock} units available"

    return f"Inventory for '{product.name}' (ID {product.id}): {status}"
