using CatalogDb;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(CatalogDbInitializer.ActivitySourceName));

builder.Services.AddSingleton<CatalogDbInitializer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CatalogDbInitializer>());
builder.Services.AddHealthChecks()
    .AddCheck<CatalogDbInitializerHealthCheck>("DbInitializer", null);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var resetDbKey = app.Configuration["DatabaseResetKey"];
    if (!string.IsNullOrEmpty(resetDbKey))
    {
        app.MapPost("/reset-db", async ([FromHeader(Name = "Authorization")] string? key, CatalogDbContext dbContext, CatalogDbInitializer dbInitializer, CancellationToken cancellationToken) =>
        {
            if (!string.Equals(key, $"Key {resetDbKey}", StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            // Delete and recreate the database. This is useful for development scenarios to reset the database to its initial state.
            await dbContext.Database.EnsureDeletedAsync(cancellationToken);
            await dbInitializer.InitializeDatabaseAsync(dbContext, cancellationToken);

            return Results.Ok();
        });
    }
}

app.MapDefaultEndpoints();

await app.RunAsync();
