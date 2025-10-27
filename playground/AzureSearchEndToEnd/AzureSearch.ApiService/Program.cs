// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using AzureSearch.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureSearchClient("search");
builder.AddAzureSearchIndexerClient("search");
var app = builder.Build();

app.MapDefaultEndpoints();

var logger = app.Logger;
// Configure the HTTP request pipeline.
app.MapGet("/", async (SearchIndexerClient searchIndexerClient, SearchIndexClient searchIndexClient, CancellationToken cancellationToken) =>
{
    logger.LogInformation("Listing indexers...");

    var indexNames = searchIndexClient.GetIndexNames(cancellationToken);
    foreach(var name in indexNames)
    {
        logger.LogInformation("Index name: {0}", name);
    }

    var indexName = "my-index";

    logger.LogInformation("Deleting if exists");
    await DeleteIfExistsAsync(logger, indexName, searchIndexClient, cancellationToken);

    logger.LogInformation("Creating index...");
    await CreateIndexAsync(logger, indexName, searchIndexClient, cancellationToken);

    var searchClient = searchIndexClient.GetSearchClient(indexName);

    logger.LogInformation("Uploading documents...");
    await UploadDocumentsAsync(logger, searchClient, cancellationToken);

    logger.LogInformation("Running queries...");
    await RunQueriesAsync(logger, searchClient, cancellationToken);
});

#pragma warning disable S6966 // Awaitable method should be used
app.Run();
#pragma warning restore S6966 // Awaitable method should be used

static async Task DeleteIfExistsAsync(ILogger logger, string indexName, SearchIndexClient searchIndexClient, CancellationToken cancellationToken)
{
    try
    {
        if (await searchIndexClient.GetIndexAsync(indexName, cancellationToken) != null)
        {
            await searchIndexClient.DeleteIndexAsync(indexName, cancellationToken);
        }
    }
    catch (RequestFailedException e) when (e.Status == 404)
    {
        //if exception occurred and status is "Not Found", this is work as expect
        logger.LogDebug(e, "Didn't find index.");
    }
}

#pragma warning disable S1172 // Unused method parameters should be removed
static async Task CreateIndexAsync(ILogger logger, string indexName, SearchIndexClient indexClient, CancellationToken cancellationToken)
{
    var fieldBuilder = new FieldBuilder();
    var searchFields = fieldBuilder.Build(typeof(Hotel));

    var definition = new SearchIndex(indexName, searchFields);

    await indexClient.CreateOrUpdateIndexAsync(definition, cancellationToken: cancellationToken);
}
#pragma warning restore S1172 // Unused method parameters should be removed

static async Task WriteDocumentsAsync(ILogger logger, SearchResults<Hotel> searchResults, CancellationToken cancellationToken)
{
    await foreach (var result in searchResults.GetResultsAsync().WithCancellation(cancellationToken))
    {
#pragma warning disable S6678 // Use PascalCase for named placeholders
        logger.LogInformation("Document {@document}", result.Document);
#pragma warning restore S6678 // Use PascalCase for named placeholders
    }
}

static async Task RunQueriesAsync(ILogger logger, SearchClient searchClient, CancellationToken cancellationToken)
{
    SearchOptions options;
    SearchResults<Hotel> results;

    logger.LogInformation("Search the entire index for the term 'motel' and return only the HotelName field:\n");

    options = new SearchOptions();
    options.Select.Add("HotelName");

    results = await searchClient.SearchAsync<Hotel>("motel", options, cancellationToken);

    await WriteDocumentsAsync(logger, results, cancellationToken);

    logger.LogInformation("Apply a filter to the index to find hotels with a room cheaper than $100 per night, and return the hotelId and description:\n");

    options = new SearchOptions()
    {
        Filter = "Rooms/any(r: r/BaseRate lt 100)"
    };
    options.Select.Add("HotelId");
    options.Select.Add("Description");

    results = await searchClient.SearchAsync<Hotel>("*", options, cancellationToken);

    await WriteDocumentsAsync(logger, results, cancellationToken);

    logger.LogInformation(
        """
        Search the entire index, order by a specific field (lastRenovationDate) in descending order, take the top two results, and show only hotelName and lastRenovationDate:\n
        """);

    options =
        new SearchOptions()
        {
            Size = 2
        };
    options.OrderBy.Add("LastRenovationDate desc");
    options.Select.Add("HotelName");
    options.Select.Add("LastRenovationDate");

    results = await searchClient.SearchAsync<Hotel>("*", options, cancellationToken);

    await WriteDocumentsAsync(logger, results, cancellationToken);

    logger.LogInformation("Search the hotel names for the term 'hotel':\n");

    options = new SearchOptions();
    options.SearchFields.Add("HotelName");

    // Adding details to select, because "Location" is not supported yet when deserializing search result to "Hotel"
    options.Select.Add("HotelId");
    options.Select.Add("HotelName");
    options.Select.Add("Description");
    options.Select.Add("Category");
    options.Select.Add("Tags");
    options.Select.Add("ParkingIncluded");
    options.Select.Add("LastRenovationDate");
    options.Select.Add("Rating");
    options.Select.Add("Address");
    options.Select.Add("Rooms");

    results = await searchClient.SearchAsync<Hotel>("hotel", options, cancellationToken);

    await WriteDocumentsAsync(logger, results, cancellationToken);
}

static async Task UploadDocumentsAsync(ILogger logger, SearchClient searchClient, CancellationToken cancellationToken)
{
    var hotelsJson = "hotels.json";
    using var openStream = File.OpenRead(hotelsJson);
    var hotels = await JsonSerializer.DeserializeAsync<List<Hotel>>(openStream, cancellationToken: cancellationToken);
    if (hotels is null)
    {
#pragma warning disable S6678 // Use PascalCase for named placeholders
        logger.LogError("Failed to import the index data from {file}", hotelsJson);
#pragma warning restore S6678 // Use PascalCase for named placeholders
        return;
    }

    var batchActions = hotels.Select(h => IndexDocumentsAction.Upload(h)).ToArray();
    var batch = IndexDocumentsBatch.Create(batchActions);

    try
    {
#pragma warning disable S1481 // Unused local variables should be removed
        IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
#pragma warning restore S1481 // Unused local variables should be removed
    }
    catch (Exception ex)
    {
        // Sometimes when your Search service is under load, indexing will fail for some of the documents in
        // the batch. Depending on your application, you can take compensating actions like delaying and
        // retrying. For this simple demo, we just log the failed document keys and continue.
        logger.LogError(ex, "Failed to index some of the documents");
    }

    logger.LogInformation("Waiting for documents to be indexed...\n");
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
}
