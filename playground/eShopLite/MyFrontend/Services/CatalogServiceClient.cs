using System.Globalization;

namespace MyFrontend.Services;

public class CatalogServiceClient(HttpClient client)
{
    public Task<Catalog?> GetItemsAsync(int? before = null, int? after = null)
    {
        // Make the query string with encoded parameters
        var query = (before, after) switch
        {
            (null, null) => default,
            (int b, null) => QueryString.Create("before", b.ToString(CultureInfo.InvariantCulture)),
            (null, int a) => QueryString.Create("after", a.ToString(CultureInfo.InvariantCulture)),
            _ => throw new InvalidOperationException(),
        };

        return client.GetFromJsonAsync<Catalog>($"api/v1/catalog/items/type/all/brand{query}");
    }
}

public record Catalog(int FirstId, int NextId, bool IsLastPage, IEnumerable<CatalogItem> Data);

public record CatalogItem
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
    public string? PictureUri { get; init; }
    public int CatalogBrandId { get; init; }
    public required string CatalogBrand { get; init; }
    public int CatalogTypeId { get; init; }
    public required string CatalogType { get; init; }
}
