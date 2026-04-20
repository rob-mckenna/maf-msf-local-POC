"""Products MCP server entry point.

Exposes mocked product catalog data via the Model Context Protocol (MCP).

Transport: stdio (default for local development).
When Azure API Management MCP support is available, this server can be
deployed as an HTTP/SSE endpoint and registered behind an APIM policy — no
code changes needed; the client switches transport via configuration.

Run with:
    python -m products_mcp_server.server
"""

from mcp.server.fastmcp import FastMCP

from products_mcp_server.tools.products_tools import (
    check_inventory,
    get_product_details,
    search_products,
)

mcp = FastMCP("ProductsMcpServer")


@mcp.tool()
def search_products_tool(query: str, max_results: int = 5) -> str:
    """Search for products by name, category, or keyword.

    Args:
        query: Search query – product name, category, or keyword
               (e.g. 'jacket', 'camping gear', 'waterproof').
        max_results: Maximum number of results to return (default 5).
    """
    return search_products(query, max_results)


@mcp.tool()
def get_product_details_tool(product_id: int) -> str:
    """Get detailed information about a product by its ID.

    Args:
        product_id: The numeric product ID (from search_products results).
    """
    return get_product_details(product_id)


@mcp.tool()
def check_inventory_tool(product_id: int) -> str:
    """Check inventory availability for a product.

    Args:
        product_id: The numeric product ID to check inventory for.
    """
    return check_inventory(product_id)


def run() -> None:
    """Entry point registered in pyproject.toml [project.scripts]."""
    mcp.run()


if __name__ == "__main__":
    run()
