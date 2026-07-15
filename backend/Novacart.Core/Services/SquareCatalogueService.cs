using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Search;
using Square;
using Square.Authentication;
using Square.Exceptions;
using Square.Models;

namespace Novacart.Api.Services;

public interface ISquareCatalogueService
{
    Task<SquareSyncResultDto> SyncProductsAsync();
}

/// <summary>
/// Narrow seam over the Square Catalog API so the real sync path can be unit-tested
/// without hitting the network. Only exposes the operation the service actually uses.
/// </summary>
public interface ISquareCatalogueGateway
{
    Task<IList<CatalogObject>> ListAsync(string types);
}

/// <summary>
/// Production gateway: builds a <see cref="SquareClient"/> from configuration and calls
/// <c>CatalogApi.ListCatalogAsync</c>. Returns an empty list when Square returns no objects.
/// </summary>
public class SquareCatalogueGateway : ISquareCatalogueGateway
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
    private readonly ILogger<SquareCatalogueGateway> _logger;

    public SquareCatalogueGateway(
        Microsoft.Extensions.Configuration.IConfiguration config,
        ILogger<SquareCatalogueGateway> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<IList<CatalogObject>> ListAsync(string types)
    {
        var accessToken = _config["Square:AccessToken"];
        var environment = _config["Square:Environment"] ?? "sandbox";

        var client = new SquareClient.Builder()
            .BearerAuthCredentials(new BearerAuthModel.Builder(accessToken).Build())
            .Environment(environment.ToLower() == "production" ? Square.Environment.Production : Square.Environment.Sandbox)
            .Build();

        var response = await client.CatalogApi.ListCatalogAsync(types: types);
        return response.Objects ?? new List<CatalogObject>();
    }
}

public class SquareSyncResultDto
{
    public int CategoriesCreated { get; set; }
    public int ProductsCreated { get; set; }
    public int ProductsUpdated { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
}

public class SquareCatalogueService : ISquareCatalogueService
{
    private readonly AppDbContext _db;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
    private readonly ILogger<SquareCatalogueService> _logger;
    private readonly ISquareCatalogueGateway _gateway;
    private readonly IProductSearchIndexer _searchIndexer;

    public SquareCatalogueService(
        AppDbContext db,
        Microsoft.Extensions.Configuration.IConfiguration config,
        ILogger<SquareCatalogueService> logger,
        ISquareCatalogueGateway gateway,
        IProductSearchIndexer searchIndexer)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _gateway = gateway;
        _searchIndexer = searchIndexer;
    }

    public async Task<SquareSyncResultDto> SyncProductsAsync()
    {
        var accessToken = _config["Square:AccessToken"];

        if (string.IsNullOrEmpty(accessToken) || accessToken.Contains("placeholder") || accessToken.Contains("YOUR_"))
        {
            _logger.LogWarning("Square Access Token is placeholder or missing. Simulating sandbox sync.");
            return await SimulateSyncAsync();
        }

        try
        {
            var result = new SquareSyncResultDto();

            // 1. Fetch categories and items from Square (via injectable gateway)
            var squareCategories = await _gateway.ListAsync("CATEGORY");
            var squareItems = await _gateway.ListAsync("ITEM");

            // Map of Square Category ID -> DB Category ID
            var categoryMap = new Dictionary<string, int>();

            // 2. Sync Categories
            foreach (var sCat in squareCategories)
            {
                if (sCat.CategoryData == null) continue;
                var catName = sCat.CategoryData.Name;
                var slug = catName.ToLower().Replace(" ", "-").Replace("/", "-");

                var existingCat = await _db.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == catName.ToLower());
                if (existingCat == null)
                {
                    existingCat = new Category { Name = catName, Slug = slug };
                    _db.Categories.Add(existingCat);
                    await _db.SaveChangesAsync();
                    result.CategoriesCreated++;
                }
                categoryMap[sCat.Id] = existingCat.Id;
            }

            // 3. Sync Products
            foreach (var sItem in squareItems)
            {
                if (sItem.ItemData == null) continue;
                var name = sItem.ItemData.Name;
                var description = sItem.ItemData.Description;
                var slug = name.ToLower().Replace(" ", "-").Replace("/", "-");

                // Get price from first variation
                decimal price = 0m;
                string currency = "AUD";
                var firstVariation = sItem.ItemData.Variations?.FirstOrDefault();
                if (firstVariation?.ItemVariationData?.PriceMoney != null)
                {
                    price = (decimal)(firstVariation.ItemVariationData.PriceMoney.Amount ?? 0L) / 100m;
                    currency = firstVariation.ItemVariationData.PriceMoney.Currency ?? "AUD";
                }

                int? dbCategoryId = null;
                if (!string.IsNullOrEmpty(sItem.ItemData.CategoryId) && categoryMap.TryGetValue(sItem.ItemData.CategoryId, out var matchedId))
                {
                    dbCategoryId = matchedId;
                }

                var existingProd = await _db.Products.FirstOrDefaultAsync(p => p.Slug == slug);
                if (existingProd != null)
                {
                    existingProd.Name = name;
                    existingProd.Description = description;
                    existingProd.Price = price;
                    existingProd.Currency = currency;
                    existingProd.CategoryId = dbCategoryId;
                    existingProd.UpdatedAt = DateTime.UtcNow;
                    _db.Products.Update(existingProd);
                    result.ProductsUpdated++;
                }
                else
                {
                    var newProd = new Product
                    {
                        Slug = slug,
                        Name = name,
                        Description = description,
                        Price = price,
                        Currency = currency,
                        StockQuantity = 15, // default stock for synced items
                        CategoryId = dbCategoryId,
                        Metadata = $"{{\"square_id\": \"{sItem.Id}\"}}"
                    };
                    _db.Products.Add(newProd);
                    result.ProductsCreated++;
                }
            }

            await _db.SaveChangesAsync();
            await ReindexSearchAsync();
            result.Message = $"Successfully synced products. Created {result.CategoriesCreated} categories, created {result.ProductsCreated} products, updated {result.ProductsUpdated} products.";
            return result;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Square API Exception occurred during sync.");
            return new SquareSyncResultDto
            {
                Success = false,
                Message = $"Square API Exception: {ex.Message} (HTTP Code: {ex.ResponseCode})"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during Square sync.");
            return new SquareSyncResultDto
            {
                Success = false,
                Message = $"Unexpected sync error: {ex.Message}"
            };
        }
    }

    private async Task<SquareSyncResultDto> SimulateSyncAsync()
    {
        var result = new SquareSyncResultDto();

        // Ensure "Square Sandbox" category exists
        var catName = "Square Sandbox";
        var catSlug = "square-sandbox";
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Name == catName);
        if (category == null)
        {
            category = new Category { Name = catName, Slug = catSlug };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            result.CategoriesCreated++;
        }

        var mockProducts = new[]
        {
            new { Name = "Square Premium Headphones", Slug = "square-premium-headphones", Price = 249.99m, Desc = "High fidelity sound from Square Catalog sandbox sync.", ImageUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=500&auto=format&fit=crop&q=60" },
            new { Name = "Square Mechanical Keyboard", Slug = "square-mechanical-keyboard", Price = 129.99m, Desc = "Tactile typing experience from Square Catalog sandbox sync.", ImageUrl = "https://images.unsplash.com/photo-1618384887929-16ec33fab9ef?w=500&auto=format&fit=crop&q=60" },
            new { Name = "Square Ergonomic Mouse", Slug = "square-ergonomic-mouse", Price = 79.99m, Desc = "Comfortable office design from Square Catalog sandbox sync.", ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=500&auto=format&fit=crop&q=60" }
        };

        foreach (var mock in mockProducts)
        {
            var existing = await _db.Products.FirstOrDefaultAsync(p => p.Slug == mock.Slug);
            if (existing != null)
            {
                existing.Name = mock.Name;
                existing.Price = mock.Price;
                existing.Description = mock.Desc;
                existing.ImageUrl = mock.ImageUrl;
                existing.CategoryId = category.Id;
                existing.UpdatedAt = DateTime.UtcNow;
                _db.Products.Update(existing);
                result.ProductsUpdated++;
            }
            else
            {
                var newProd = new Product
                {
                    Name = mock.Name,
                    Slug = mock.Slug,
                    Price = mock.Price,
                    Description = mock.Desc,
                    Currency = "AUD",
                    StockQuantity = 20,
                    CategoryId = category.Id,
                    ImageUrl = mock.ImageUrl,
                    Metadata = "{\"square_id\": \"mock_square_id\"}"
                };
                _db.Products.Add(newProd);
                result.ProductsCreated++;
            }
        }

        await _db.SaveChangesAsync();
        await ReindexSearchAsync();
        result.Message = $"[Simulation Mode] Synced Square sandbox catalogue. Created {result.CategoriesCreated} categories, created {result.ProductsCreated} products, updated {result.ProductsUpdated} products.";
        return result;
    }

    private async Task ReindexSearchAsync()
    {
        if (!_searchIndexer.IsEnabled)
            return;

        try
        {
            await _searchIndexer.ReindexAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch reindex after Square sync failed.");
        }
    }
}
