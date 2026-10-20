using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Aspire injects the connection string of the "db" database.
builder.AddNpgsqlDataSource("db");

// Aspire injects the address of the catalog service.
builder.Services.AddHttpClient("catalog", client => client.BaseAddress = new Uri(builder.Configuration["CATALOG_HTTP"]!));

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
        .AddHttpClientInstrumentation())
    .UseOtlpExporter();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Outside of "aspire run", such as in Docker Compose, the database must be created.
var server = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("db")) { Database = "postgres" };
await using (var connection = new NpgsqlConnection(server.ConnectionString))
{
    await connection.OpenAsync();
    await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'db'", connection);
    if (await exists.ExecuteScalarAsync() is null)
    {
        await using var create = new NpgsqlCommand("CREATE DATABASE db", connection);
        await create.ExecuteNonQueryAsync();
    }
}

await using (var command = app.Services.GetRequiredService<NpgsqlDataSource>().CreateCommand("""
    CREATE TABLE IF NOT EXISTS orders (id serial PRIMARY KEY, product text NOT NULL, quantity int NOT NULL, created timestamptz NOT NULL DEFAULT now());
    INSERT INTO orders (product, quantity) SELECT 'Trail Backpack', 2 WHERE NOT EXISTS (SELECT 1 FROM orders);
    """))
{
    await command.ExecuteNonQueryAsync();
}

app.MapGet("/api/products", async (IHttpClientFactory factory) =>
{
    var response = await factory.CreateClient("catalog").GetStringAsync("/products");
    return Results.Content(response, "application/json");
});

app.MapGet("/api/orders", async (NpgsqlDataSource db) =>
{
    await using var command = db.CreateCommand("SELECT id, product, quantity, created FROM orders ORDER BY id DESC LIMIT 5");
    await using var reader = await command.ExecuteReaderAsync();
    var orders = new List<Order>();
    while (await reader.ReadAsync())
    {
        orders.Add(new Order(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetDateTime(3)));
    }
    return orders;
});

app.MapPost("/api/orders", async (NewOrder order, NpgsqlDataSource db, ILogger<Program> logger) =>
{
    await using var command = db.CreateCommand("INSERT INTO orders (product, quantity) VALUES ($1, $2) RETURNING id");
    command.Parameters.AddWithValue(order.Product);
    command.Parameters.AddWithValue(order.Quantity);
    var id = (int)(await command.ExecuteScalarAsync())!;
    logger.LogInformation("Order {OrderId} placed for {Quantity} x {Product}", id, order.Quantity, order.Product);
    return Results.Created($"/api/orders/{id}", new { id });
});

app.MapHealthChecks("/health");

app.Run();

record Order(int Id, string Product, int Quantity, DateTime Created);

record NewOrder(string Product, int Quantity);
