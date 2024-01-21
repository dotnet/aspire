// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

var sessionId = Guid.NewGuid().ToString();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosDB("db", settings =>
{
    settings.IgnoreEmulatorCertificate = true;
});

var app = builder.Build();

app.MapGet("/", async (CosmosClient cosmosClient) =>
{
    var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync("db").ConfigureAwait(false)).Database;
    var container = (await db.CreateContainerIfNotExistsAsync("entries", "/sessionId").ConfigureAwait(false)).Container;

    // Add an entry to the database on each request.
    var newEntry = new Entry() { Id = Guid.NewGuid().ToString(), SessionId = Guid.NewGuid().ToString() };
    await container.CreateItemAsync(newEntry).ConfigureAwait(false);

    var entries = new List<Entry>();
    var iterator = container.GetItemQueryIterator<Entry>(requestOptions: new QueryRequestOptions() { MaxItemCount = 5 });

    var batchCount = 0;
    while (iterator.HasMoreResults)
    {
        batchCount++;
        var batch = await iterator.ReadNextAsync().ConfigureAwait(false);
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

app.Run();

public class Entry
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("sessionId")]
    public string? SessionId { get; set; }
}
