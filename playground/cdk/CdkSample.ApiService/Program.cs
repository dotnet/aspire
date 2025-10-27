// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure.Data.AppConfiguration;
using Azure.Messaging.ServiceBus;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureBlobServiceClient("blobs");
builder.AddSqlServerDbContext<SqlContext>("sqldb");
builder.AddAzureKeyVaultClient("mykv");
builder.AddRedisClient("cache");
builder.AddCosmosDbContext<CosmosContext>("cosmos", "cosmosdb");
builder.AddNpgsqlDbContext<NpgsqlContext>("pgsqldb");
builder.AddAzureServiceBusClient("servicebus");
builder.AddAzureSearchClient("search");
builder.Services.AddSignalR().AddNamedAzureSignalR("signalr");

builder.Services.AddAzureClients(clients =>
{
    clients.AddConfigurationClient(new Uri(builder.Configuration.GetConnectionString("appConfig")!));
});

var app = builder.Build();

app.MapGet("/", async (BlobServiceClient bsc, SqlContext sqlContext, SecretClient sc, IConnectionMultiplexer connection, CosmosContext cosmosContext, NpgsqlContext npgsqlContext, ServiceBusClient sbc, ConfigurationClient cc, SearchIndexClient search) =>
{
    return new
    {
        cosmosDocuments = await TestCosmosAsync(cosmosContext),
        redisEntries = await TestRedisAsync(connection),
        secretChecked = await TestSecretAsync(sc),
        blobFiles = await TestBlobStorageAsync(bsc),
        sqlRows = await TestSqlServerAsync(sqlContext),
        npgsqlRows = await TestNpgsqlAsync(npgsqlContext),
        serviceBus = await TestServiceBusAsync(sbc),
        search = await TestSearchAsync(search),
        appConfig = await TestAppConfig(cc)
    };
});
app.Run();

static async Task<string> TestAppConfig(ConfigurationClient cc)
{
    var setting = new ConfigurationSetting("ConfigSection:ConfigKey", "ConfigValue");
    ConfigurationSetting updatedSetting = await cc.SetConfigurationSettingAsync(setting);
    return updatedSetting.ETag.ToString();
}

static async Task<IEnumerable<Entry>> TestRedisAsync(IConnectionMultiplexer connection)
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

}

static async Task<bool> TestSecretAsync(SecretClient secretClient)
{
    KeyVaultSecret s = await secretClient.GetSecretAsync("mysecret");
    return s.Value == "open sesame";
}

static async Task<List<string>> TestBlobStorageAsync(BlobServiceClient bsc)
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
}

static async Task<ServiceBusReceivedMessage> TestServiceBusAsync(ServiceBusClient sbc)
{
    await using var sender = sbc.CreateSender("queue1");
    await sender.SendMessageAsync(new ServiceBusMessage("Hello, World!"));

    await using var receiver = sbc.CreateReceiver("queue1");
    return await receiver.ReceiveMessageAsync();
}

static async Task<List<Entry>> TestSqlServerAsync(SqlContext context)
{
    await context.Database.EnsureCreatedAsync();

    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();
    return entries;
}

static async Task<List<Entry>> TestNpgsqlAsync(NpgsqlContext context)
{
    await context.Database.EnsureCreatedAsync();

    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();
    return entries;
}

static async Task<List<Entry>> TestCosmosAsync(CosmosContext context)
{
    await context.Database.EnsureCreatedAsync();

    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();
    return entries;
}

static async Task<SearchResourceCounter> TestSearchAsync(SearchIndexClient search)
{
    // Build the index
    const string IndexName = "mydocs";
    var fields = new FieldBuilder().Build(typeof(SimpleDocument));
    SearchIndex index = new(IndexName, fields);
    index = await search.CreateOrUpdateIndexAsync(index);

    // Upload a few documents
    var entries = search.GetSearchClient(IndexName);
    await entries.UploadDocumentsAsync(
        [
            new SimpleDocument($"Foo:{DateTimeOffset.UtcNow}"),
            new SimpleDocument($"Bar:{DateTimeOffset.UtcNow}"),
            new SimpleDocument($"Baz:{DateTimeOffset.UtcNow}")
        ],
        new() { ThrowOnAnyError = true });

    // Get the service stats that probably won't show the documents
    // right away, but you'd eventually see them increment on multiple requests
    SearchServiceStatistics stats = await search.GetServiceStatisticsAsync();
    return stats.Counters.DocumentCounter;
}

public class SimpleDocument(string name)
{
    [SimpleField(IsKey = true)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [SearchableField]
    public string? Name { get; set; } = name;
}

public class NpgsqlContext(DbContextOptions<NpgsqlContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public class SqlContext(DbContextOptions<SqlContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public class CosmosContext(DbContextOptions<CosmosContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
