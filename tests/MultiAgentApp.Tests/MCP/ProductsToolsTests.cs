using ProductsMcpServer.Tools;

namespace MultiAgentApp.Tests.MCP;

/// <summary>
/// Tests for the mocked Products MCP tools.
/// These tests exercise the tool logic directly (no MCP transport required).
/// </summary>
public class ProductsToolsTests
{
    private readonly ProductsTools _tools = new();

    [Theory]
    [InlineData("jacket")]
    [InlineData("camping")]
    [InlineData("waterproof")]
    [InlineData("shoes")]
    public void SearchProducts_MatchingKeyword_ReturnsResults(string keyword)
    {
        var result = _tools.SearchProducts(keyword);

        Assert.NotNull(result);
        Assert.DoesNotContain("No products found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchProducts_NoMatch_ReturnsNotFoundMessage()
    {
        var result = _tools.SearchProducts("xyznonexistentproduct123");

        Assert.Contains("No products found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchProducts_EmptyQuery_ReturnsErrorMessage()
    {
        var result = _tools.SearchProducts(string.Empty);

        Assert.Contains("Please provide a search query", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchProducts_RespectsMaxResultsParameter()
    {
        var result = _tools.SearchProducts("outdoor", maxResults: 2);

        Assert.NotNull(result);
        // Should show at most 2 results – count the product lines
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.TrimStart().StartsWith('['))
            .ToList();
        Assert.True(lines.Count <= 2, $"Expected at most 2 results but got {lines.Count}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void GetProductDetails_ValidId_ReturnsDetails(int productId)
    {
        var result = _tools.GetProductDetails(productId);

        Assert.NotNull(result);
        Assert.Contains("Product Details", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"ID", result);
        Assert.Contains("Price", result);
    }

    [Fact]
    public void GetProductDetails_InvalidId_ReturnsNotFoundMessage()
    {
        var result = _tools.GetProductDetails(9999);

        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void CheckInventory_ValidId_ReturnsStockStatus(int productId)
    {
        var result = _tools.CheckInventory(productId);

        Assert.NotNull(result);
        Assert.Contains("Inventory", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckInventory_InvalidId_ReturnsNotFoundMessage()
    {
        var result = _tools.CheckInventory(99999);

        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchProducts_MultiWordQuery_ReturnsRelevantResults()
    {
        var result = _tools.SearchProducts("waterproof jacket");

        Assert.NotNull(result);
        Assert.DoesNotContain("No products found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetProductDetails_ContainsPriceInfo()
    {
        var result = _tools.GetProductDetails(1);

        Assert.Contains("Price", result);
        Assert.Contains("$", result);
    }
}
