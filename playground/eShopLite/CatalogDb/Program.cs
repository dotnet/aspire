using CatalogDb;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("catalogdb")
    ?? throw new InvalidOperationException("Connection string is not configured.");

// a workaround for https://github.com/npgsql/efcore.pg/issues/2821
var configureNpgsqlLogging = (NpgsqlDataSourceBuilder builder) => { builder.UseLoggerFactory(null); };
builder.Services.AddNpgsqlDataSource(connectionString, configureNpgsqlLogging)
    .AddDbContextPool<CatalogDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseNpgsql())
    .EnrichNpgsqlEntityFrameworkCore<CatalogDbContext>(builder);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(CatalogDbInitializer.ActivitySourceName));

builder.Services.AddSingleton<CatalogDbInitializer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CatalogDbInitializer>());
builder.Services.AddHealthChecks()
    .AddCheck<CatalogDbInitializerHealthCheck>("DbInitializer", null);

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();

