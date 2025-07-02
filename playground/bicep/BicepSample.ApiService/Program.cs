// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<MyDbContext>("db");
builder.AddNpgsqlDbContext<MyPgDbContext>("db2");
builder.AddAzureCosmosClient("cosmos");
builder.AddRedisClient("redis");
builder.AddAzureBlobClient("blob");
builder.AddAzureTableClient("table");
builder.AddAzureQueueServiceClient("queue");
builder.AddAzureServiceBusClient("sb");

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

app.MapGet("/blobs", async (BlobServiceClient blobServiceClient) =>
{
    var container = blobServiceClient.GetBlobContainerClient("blobs");

    await container.CreateIfNotExistsAsync();

    var entry = new Entry();

    var blob = container.GetBlobClient(entry.Id.ToString());

    // Add an entry to the blob on each request.
    await blob.UploadAsync(new BinaryData(entry));

    var entries = new List<Entry>();
    await foreach (var item in container.GetBlobsAsync())
    {
        var client = container.GetBlobClient(item.Name);
        using var content = await client.OpenReadAsync();
        entries.Add(JsonSerializer.Deserialize<Entry>(content)!);
    }

    return entries;
});

app.MapGet("/tables", async (TableServiceClient tableServiceClient) =>
{
    var table = tableServiceClient.GetTableClient("entries");

    await table.CreateIfNotExistsAsync();

    var entry = new Entry();

    var tableEntry = new TableEntity(entry.Id.ToString(), entry.Id.ToString())
    {
        { "data", JsonSerializer.Serialize(entry) }
    };

    // Add an entry to the table on each request.
    await table.AddEntityAsync(tableEntry);

    var entries = new List<Entry>();
    await foreach (var item in table.QueryAsync<TableEntity>())
    {
        entries.Add(JsonSerializer.Deserialize<Entry>((string)item["data"])!);
    }

    return entries;
});

app.MapGet("/queues", async (QueueServiceClient queueServiceClient, CancellationToken cancellationToken) =>
{
    var queue = queueServiceClient.GetQueueClient("entries");

    await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

    var entry = new Entry();

    // Add an entry to the queue on each request.
    await queue.SendMessageAsync(new BinaryData(entry), cancellationToken: cancellationToken);

    var entries = new List<Entry>();

    var message = await queue.ReceiveMessageAsync(cancellationToken: cancellationToken);
    if (message != null)
    {
        entries.Add(message.Value.Body.ToObjectFromJson<Entry>()!);
    }

    return entries;
});

app.MapGet("/servicebus", async (ServiceBusClient serviceBusClient, CancellationToken cancellationToken) =>
{
    await using var sender = serviceBusClient.CreateSender("queue1");
    var entry = new Entry();

    // Add an entry to the queue on each request.
    await sender.SendMessageAsync(new ServiceBusMessage(new BinaryData(entry)), cancellationToken);

    var entries = new List<Entry>();

    await using var receiver = serviceBusClient.CreateReceiver("queue1");
    var message = await receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);
    if (message != null)
    {
        entries.Add(message.Body.ToObjectFromJson<Entry>()!);
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
