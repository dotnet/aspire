// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapper;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.AddMySqlDataSource("Catalog");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();
app.MapGet("/catalog", async (MySqlConnection db) =>
{
    const string sql = """
                SELECT Id, Name, Description, Price
                FROM catalog
                """;

    return await db.QueryAsync<CatalogItem>(sql);
});

app.MapGet("/catalog/{id}", async (int id, MySqlConnection db) =>
{
    const string sql = """
                SELECT Id, Name, Description, Price
                FROM catalog
                WHERE Id = @id
                """;

    return await db.QueryFirstOrDefaultAsync<CatalogItem>(sql, new { id }) is { } item
        ? Results.Ok(item)
        : Results.NotFound();
});

app.MapPost("/catalog", async (CatalogItem item, MySqlConnection db) =>
{
    const string sql = """
                INSERT INTO catalog (Name, Description, Price)
                VALUES (@Name, @Description, @Price);
                SELECT LAST_INSERT_ID();
                """;

    var id = await db.ExecuteScalarAsync<int>(sql, item);
    return Results.Created($"/catalog/{id}", id);
});

app.MapDelete("/catalog/{id}", async (int id, MySqlConnection db) =>
{
    const string sql = """
                DELETE FROM catalog
                WHERE Id = @id
                """;

    var rows = await db.ExecuteAsync(sql, new { id });
    return rows > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

public record CatalogItem(int Id, string Name, string Description, decimal Price);
