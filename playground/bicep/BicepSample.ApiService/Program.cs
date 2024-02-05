// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<MyDbContext>("db");
builder.AddNpgsqlDbContext<MyPgDbContext>("db2");
builder.AddAzureCosmosDB("db3");
builder.AddRedis("redis");

var app = builder.Build();

app.MapGet("/", async (MyDbContext context) =>
{
    // You wouldn't normally do this on every call,
    // but doing it here just to make this simple.
    context.Database.EnsureCreated();

    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();

    return new
    {
        totalEntries = entries.Count,
        entries = entries
    };
});

app.MapGet("/pg", async (MyPgDbContext context) =>
{
    // You wouldn't normally do this on every call,
    // but doing it here just to make this simple.
    context.Database.EnsureCreated();

    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();

    return new
    {
        totalEntries = entries.Count,
        entries = entries
    };
});

app.MapGet("/cosmos", async (CosmosClient cosmosClient) =>
{
    var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync("db3")).Database;
    var container = (await db.CreateContainerIfNotExistsAsync("entries", "/Id")).Container;

    // Add an entry to the database on each request.
    var newEntry = new Entry();
    await container.CreateItemAsync(newEntry);

    var entries = new List<Entry>();
    var iterator = container.GetItemQueryIterator<Entry>(requestOptions: new QueryRequestOptions() { MaxItemCount = 5 });

    var batchCount = 0;
    while (iterator.HasMoreResults)
    {
        batchCount++;
        var batch = await iterator.ReadNextAsync();
        foreach (var entry in batch)
        {
            entries.Add(entry);
        }
    }

    return new
    {
        batchCount = batchCount,
        totalEntries = entries.Count,
        entries = entries
    };
});

app.MapGet("/redis", async (IConnectionMultiplexer connection) =>
{
    var database = connection.GetDatabase();

    var entry = new Entry();

    // Add an entry to the list on each request.
    await database.ListRightPushAsync("entries", JsonSerializer.SerializeToUtf8Bytes(entry));

    var entries = new List<Entry>();
    var list = await database.ListRangeAsync("entries");

    foreach (var item in list)
    {
        entries.Add(JsonSerializer.Deserialize<Entry>((string)item!)!);
    }

    return entries;
});

app.Run();

public class MyPgDbContext(DbContextOptions<MyPgDbContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public class Entry
{
    [Newtonsoft.Json.JsonProperty("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
}
