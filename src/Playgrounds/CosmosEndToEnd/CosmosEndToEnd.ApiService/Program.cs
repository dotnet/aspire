// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using CosmosEndToEnd.ApiService;
using Microsoft.Azure.Cosmos;

var sessionId = Guid.NewGuid().ToString();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosDB("db",
                         settings =>
                            {
                                settings.IgnoreEmulatorCertificate = true;
                            },
                         clientOptions =>
                         {
                             // Default serializer for Cosmos V3 client is JSON.NET, this changes
                             // us to use S.T.J for this playground.
                             clientOptions.Serializer = new StjSerializer(new System.Text.Json.JsonSerializerOptions());
                         });

var app = builder.Build();

app.MapGet("/", async (CosmosClient cosmosClient) =>
{
    var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync("db").ConfigureAwait(false)).Database;
    var container = (await db.CreateContainerIfNotExistsAsync("entries", "/sessionId").ConfigureAwait(false)).Container;

    // Add an entry to the database on each request.
    await container.CreateItemAsync(new Entry(Guid.NewGuid().ToString(), sessionId)).ConfigureAwait(false);

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

public class Entry(string id, string sessionId)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = sessionId;
}
