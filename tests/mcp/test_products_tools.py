"""Tests for products MCP tool logic (no MCP transport required)."""

import pytest

from products_mcp_server.tools.products_tools import (
    check_inventory,
    get_product_details,
    search_products,
)


@pytest.mark.parametrize("keyword", ["jacket", "camping", "waterproof", "shoes"])
def test_search_products_matching_keyword_returns_results(keyword: str):
    result = search_products(keyword)

    assert result is not None
    assert "no products found" not in result.lower()


def test_search_products_no_match_returns_not_found_message():
    result = search_products("xyznonexistentproduct123")

    assert "no products found" in result.lower()


def test_search_products_empty_query_returns_error_message():
    result = search_products("")

    assert "please provide a search query" in result.lower()


def test_search_products_respects_max_results_parameter():
    result = search_products("outdoor", max_results=2)

    assert result is not None
    lines = [
        line for line in result.splitlines() if line.strip().startswith("[")
    ]
    assert len(lines) <= 2, f"Expected at most 2 results but got {len(lines)}"


@pytest.mark.parametrize("product_id", [1, 3, 5])
def test_get_product_details_valid_id_returns_details(product_id: int):
    result = get_product_details(product_id)

    assert result is not None
    assert "product details" in result.lower()
    assert "ID" in result
    assert "Price" in result


def test_get_product_details_invalid_id_returns_not_found():
    result = get_product_details(9999)

    assert "not found" in result.lower()


@pytest.mark.parametrize("product_id", [1, 2, 10])
def test_check_inventory_valid_id_returns_stock_status(product_id: int):
    result = check_inventory(product_id)

    assert result is not None
    assert "inventory" in result.lower()


def test_check_inventory_invalid_id_returns_not_found():
    result = check_inventory(99999)

    assert "not found" in result.lower()


def test_search_products_multi_word_query_returns_relevant_results():
    result = search_products("waterproof jacket")

    assert result is not None
    assert "no products found" not in result.lower()


def test_get_product_details_contains_price_info():
    result = get_product_details(1)

    assert "Price" in result
    assert "$" in result
