// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCosmosDbContext<TestCosmosContext>("account", "db");
builder.AddAzureBlobClient("blobs");
builder.AddRedisClient("cache");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () =>
{
    return Results.Content("""
    <html>
        <body>
            <ul>
                <li><a href="/blobs">Blobs</a></li>
                <li><a href="/cosmos">Cosmos</a></li>
                <li><a href="/redis/ping">Redis Ping</a></li>
                <li><a href="/redis/set">Redis Set</a></li>
                <li><a href="/redis/get">Redis Get</a></li>
            </ul>
        </body>
    </html>
    """,
    "text/html");
});

app.MapGet("/blobs", async (BlobServiceClient bsc) =>
{
    var container = bsc.GetBlobContainerClient("mycontainer");
    await container.CreateIfNotExistsAsync();

    var blobNameAndContent = Guid.NewGuid().ToString();
    await container.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    var blobs = container.GetBlobsAsync();

    var blobNames = new List<string>();

    await foreach (var blob in blobs)
    {
        blobNames.Add(blob.Name);
    }

    return blobNames;
});

app.MapGet("/cosmos", async (TestCosmosContext context) =>
{
    await context.Database.EnsureCreatedAsync();

    context.Entries.Add(new EntityFrameworkEntry());
    await context.SaveChangesAsync();

    return await context.Entries.ToListAsync();
});

app.MapGet("/redis/ping", async (IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().PingAsync();
});

app.MapGet("/redis/set", async (IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().StringSetAsync("Key", $"{DateTime.Now}");
});

app.MapGet("/redis/get", async (IConnectionMultiplexer connection) =>
{
    var redisValue = await connection.GetDatabase().StringGetAsync("Key");
    return redisValue.HasValue ? redisValue.ToString() : "(null)";
});

app.Run();

public class Entry
{
    [JsonProperty("id")]
    public string? Id { get; set; }
}

public class TestCosmosContext(DbContextOptions<TestCosmosContext> options) : DbContext(options)
{
    public DbSet<EntityFrameworkEntry> Entries { get; set; }
}

public class EntityFrameworkEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

