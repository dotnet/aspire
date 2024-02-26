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
builder.AddAzureSearch("search");

var app = builder.Build();

app.MapDefaultEndpoints();

var logger = app.Logger;
// Configure the HTTP request pipeline.
app.MapGet("/", async (SearchIndexClient searchIndexClient, CancellationToken cancellationToken) =>
{
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

app.Run();

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

static async Task CreateIndexAsync(ILogger logger, string indexName, SearchIndexClient indexClient, CancellationToken cancellationToken)
{
    var fieldBuilder = new FieldBuilder();
    var searchFields = fieldBuilder.Build(typeof(Hotel));

    var definition = new SearchIndex(indexName, searchFields);

    await indexClient.CreateOrUpdateIndexAsync(definition, cancellationToken: cancellationToken);
}

static async Task WriteDocumentsAsync(ILogger logger, SearchResults<Hotel> searchResults, CancellationToken cancellationToken)
{
    await foreach (var result in searchResults.GetResultsAsync().WithCancellation(cancellationToken))
    {
        logger.LogInformation("Document {@document}", result.Document);
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

    //Adding details to select, because "Location" is not supported yet when deserialize search result to "Hotel"
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
        logger.LogError("Failed to import the index data from {file}", hotelsJson);
        return;
    }

    var batchActions = hotels.Select(h => IndexDocumentsAction.Upload(h)).ToArray();
    var batch = IndexDocumentsBatch.Create(batchActions);

    try
    {
        IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
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
