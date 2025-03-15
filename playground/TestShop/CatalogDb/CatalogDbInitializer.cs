using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace CatalogDb;

internal sealed class CatalogDbInitializer(IServiceProvider serviceProvider, ILogger<CatalogDbInitializer> logger)
    : BackgroundService
{
    public const string ActivitySourceName = "Migrations";

    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        using var activity = _activitySource.StartActivity("Initializing catalog database", ActivityKind.Client);
        await InitializeDatabaseAsync(dbContext, cancellationToken);
    }

    public async Task InitializeDatabaseAsync(CatalogDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(dbContext.Database.MigrateAsync, cancellationToken);

        await SeedAsync(dbContext, cancellationToken);

        logger.LogInformation("Database initialization completed after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
    }

    private async Task SeedAsync(CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding database");

        static List<CatalogBrand> GetPreconfiguredCatalogBrands()
        {
            return [
                new() { Brand = "Azure" },
                new() { Brand = ".NET" },
                new() { Brand = "Visual Studio" },
                new() { Brand = "SQL Server" },
                new() { Brand = "Other" }
            ];
        }

        static List<CatalogType> GetPreconfiguredCatalogTypes()
        {
            return [
                new() { Type = "Mug" },
                new() { Type = "T-Shirt" },
                new() { Type = "Sheet" },
                new() { Type = "USB Memory Stick" }
            ];
        }

        static List<CatalogItem> GetPreconfiguredItems(DbSet<CatalogBrand> catalogBrands, DbSet<CatalogType> catalogTypes)
        {
            var dotNet = catalogBrands.First(b => b.Brand == ".NET");
            var other = catalogBrands.First(b => b.Brand == "Other");

            var mug = catalogTypes.First(c => c.Type == "Mug");
            var tshirt = catalogTypes.First(c => c.Type == "T-Shirt");
            var sheet = catalogTypes.First(c => c.Type == "Sheet");

            return [
                new() { CatalogType = tshirt, CatalogBrand = dotNet, AvailableStock = 100, Description = ".NET Bot Black Hoodie", Name = ".NET Bot Black Hoodie", Price = 19.5M, PictureFileName = "1.png" },
                new() { CatalogType = mug, CatalogBrand = dotNet, AvailableStock = 100, Description = ".NET Black & White Mug", Name = ".NET Black & White Mug", Price= 8.50M, PictureFileName = "2.png" },
                new() { CatalogType = tshirt, CatalogBrand = other, AvailableStock = 100, Description = "Prism White T-Shirt", Name = "Prism White T-Shirt", Price = 12, PictureFileName = "3.png" },
                new() { CatalogType = tshirt, CatalogBrand = dotNet, AvailableStock = 100, Description = ".NET Foundation T-shirt", Name = ".NET Foundation T-shirt", Price = 12, PictureFileName = "4.png" },
                new() { CatalogType = sheet, CatalogBrand = other, AvailableStock = 100, Description = "Roslyn Red Sheet", Name = "Roslyn Red Sheet", Price = 8.5M, PictureFileName = "5.png" },
                new() { CatalogType = tshirt, CatalogBrand = dotNet, AvailableStock = 100, Description = ".NET Blue Hoodie", Name = ".NET Blue Hoodie", Price = 12, PictureFileName = "6.png" },
                new() { CatalogType = tshirt, CatalogBrand = other, AvailableStock = 100, Description = "Roslyn Red T-Shirt", Name = "Roslyn Red T-Shirt", Price = 12, PictureFileName = "7.png" },
                new() { CatalogType = tshirt, CatalogBrand = other, AvailableStock = 100, Description = "Kudu Purple Hoodie", Name = "Kudu Purple Hoodie", Price = 8.5M, PictureFileName = "8.png" },
                new() { CatalogType = mug, CatalogBrand = other, AvailableStock = 100, Description = "Cup<T> White Mug", Name = "Cup<T> White Mug", Price = 12, PictureFileName = "9.png" },
                new() { CatalogType = sheet, CatalogBrand = dotNet, AvailableStock = 100, Description = ".NET Foundation Sheet", Name = ".NET Foundation Sheet", Price = 12, PictureFileName = "10.png" },
                new() { CatalogType = sheet, CatalogBrand = dotNet, AvailableStock = 100, Description = "Cup<T> Sheet", Name = "Cup<T> Sheet", Price = 8.5M, PictureFileName = "11.png" },
                new() { CatalogType = tshirt, CatalogBrand = other, AvailableStock = 100, Description = "Prism White TShirt", Name = "Prism White TShirt", Price = 12, PictureFileName = "12.png" }
            ];
        }

        if (!dbContext.CatalogBrands.Any())
        {
            var brands = GetPreconfiguredCatalogBrands();
            await dbContext.CatalogBrands.AddRangeAsync(brands, cancellationToken);

            logger.LogInformation("Seeding {CatalogBrandCount} catalog brands", brands.Count);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!dbContext.CatalogTypes.Any())
        {
            var types = GetPreconfiguredCatalogTypes();
            await dbContext.CatalogTypes.AddRangeAsync(types, cancellationToken);

            logger.LogInformation("Seeding {CatalogTypeCount} catalog item types", types.Count);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!dbContext.CatalogItems.Any())
        {
            var items = GetPreconfiguredItems(dbContext.CatalogBrands, dbContext.CatalogTypes);
            await dbContext.CatalogItems.AddRangeAsync(items, cancellationToken);

            logger.LogInformation("Seeding {CatalogItemCount} catalog items", items.Count);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
