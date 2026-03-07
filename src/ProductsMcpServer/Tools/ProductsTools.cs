using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ProductsMcpServer.Tools;

/// <summary>
/// MCP tools that provide mocked product catalog and inventory data.
///
/// These tools simulate a real Products API integration.
/// In production, replace the mock data with real HTTP calls to a product catalog service
/// (e.g. a REST API backed by Azure Cosmos DB or SQL Database).
///
/// When Azure API Management MCP is available, these tools will be hosted behind
/// an APIM policy and the client will switch from stdio to HTTP/SSE transport.
/// </summary>
[McpServerToolType]
public sealed class ProductsTools
{
    private static readonly List<Product> MockProducts =
    [
        new(1, "Alpine Waterproof Jacket", "Outerwear", 189.99m, 42, ["waterproof", "jacket", "outdoor", "camping", "hiking"], "Lightweight, fully waterproof jacket with sealed seams. 3-layer Gore-Tex® construction."),
        new(2, "Trail Running Shoes", "Footwear", 129.99m, 18, ["shoes", "trail", "running", "outdoor"], "Aggressive grip outsole with rock plate protection. Waterproof upper."),
        new(3, "4-Season Tent", "Camping", 449.00m, 7, ["tent", "camping", "shelter", "outdoor"], "Freestanding 3-pole design rated for 4 seasons. Sleeps 2 adults comfortably."),
        new(4, "Sleeping Bag -10°C", "Camping", 219.00m, 15, ["sleeping bag", "camping", "outdoor"], "Down-filled sleeping bag rated to -10°C / 14°F. Packable to 4L."),
        new(5, "Trekking Poles Set", "Accessories", 79.99m, 30, ["poles", "trekking", "hiking", "camping"], "Collapsible aluminium poles with cork grips and tungsten carbide tips."),
        new(6, "Headlamp 350 Lumens", "Accessories", 49.99m, 55, ["headlamp", "light", "camping", "outdoor"], "Rechargeable USB-C headlamp with red light mode. IPX4 water resistant."),
        new(7, "Merino Wool Base Layer", "Clothing", 89.99m, 24, ["wool", "base layer", "merino", "clothing"], "Natural temperature regulation. Odour resistant. Machine washable."),
        new(8, "Insulated Thermos 1L", "Accessories", 34.99m, 60, ["thermos", "bottle", "camping", "outdoor"], "Double-wall vacuum insulation. Keeps liquids hot 12 hrs / cold 24 hrs."),
        new(9, "Rain Pants", "Outerwear", 79.99m, 20, ["rain", "pants", "waterproof", "outdoor"], "Lightweight and packable waterproof trousers with full-length zips."),
        new(10, "Camp Stove Compact", "Camping", 59.99m, 12, ["stove", "camping", "cooking", "outdoor"], "Compact canister stove. Boils 1L in 3 minutes. 185g packed weight."),
    ];

    /// <summary>
    /// Searches for products by name or category keyword.
    /// </summary>
    [McpServerTool(Name = "search_products"), Description("Search for products by name, category, or keyword.")]
    public string SearchProducts(
        [Description("Search query – product name, category, or keyword (e.g. 'jacket', 'camping gear', 'waterproof')")]
        string query,
        [Description("Maximum number of results to return (default 5)")]
        int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Please provide a search query.";
        }

        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var results = MockProducts
            .Where(p =>
                terms.Any(t =>
                    p.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                    p.Tags.Any(tag => tag.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Take(maxResults)
            .ToList();

        if (results.Count == 0)
        {
            return $"No products found for '{query}'.";
        }

        var lines = results.Select(p =>
            $"  [{p.Id}] {p.Name}  |  {p.Category}  |  ${p.Price:F2}  |  " +
            $"{(p.Stock > 0 ? $"In stock ({p.Stock})" : "Out of stock")}");

        return $"Found {results.Count} product(s) matching '{query}':\n" + string.Join("\n", lines);
    }

    /// <summary>
    /// Gets detailed information about a specific product by ID.
    /// </summary>
    [McpServerTool(Name = "get_product_details"), Description("Get detailed information about a product by its ID.")]
    public string GetProductDetails(
        [Description("The numeric product ID (from search_products results)")]
        int productId)
    {
        var product = MockProducts.FirstOrDefault(p => p.Id == productId);

        if (product is null)
        {
            return $"Product with ID {productId} not found.";
        }

        return $"""
                Product Details:
                  ID          : {product.Id}
                  Name        : {product.Name}
                  Category    : {product.Category}
                  Price       : ${product.Price:F2}
                  Stock       : {(product.Stock > 0 ? $"{product.Stock} units available" : "Out of stock")}
                  Description : {product.Description}
                  Tags        : {string.Join(", ", product.Tags)}
                """;
    }

    /// <summary>
    /// Checks the inventory level for a product.
    /// </summary>
    [McpServerTool(Name = "check_inventory"), Description("Check inventory availability for a product.")]
    public string CheckInventory(
        [Description("The numeric product ID to check inventory for")]
        int productId)
    {
        var product = MockProducts.FirstOrDefault(p => p.Id == productId);

        if (product is null)
        {
            return $"Product with ID {productId} not found.";
        }

        var status = product.Stock switch
        {
            0 => "❌ Out of stock",
            <= 5 => $"⚠️  Low stock – only {product.Stock} unit(s) remaining",
            <= 15 => $"✅ In stock – {product.Stock} units available",
            _ => $"✅ Well stocked – {product.Stock} units available"
        };

        return $"Inventory for '{product.Name}' (ID {product.Id}): {status}";
    }

    // ── Internal data model ──────────────────────────────────────────────────

    private sealed record Product(
        int Id,
        string Name,
        string Category,
        decimal Price,
        int Stock,
        string[] Tags,
        string Description);
}
