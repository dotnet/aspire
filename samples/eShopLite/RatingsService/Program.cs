// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using RatingsService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureCosmosDB("ratingsdb");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var cc = app.Services.GetRequiredService<CosmosClient>();
var dbr = await cc.CreateDatabaseIfNotExistsAsync("ratingsdb");
var cr = await dbr.Database.CreateContainerIfNotExistsAsync("ratings", "/ProductId");
var ir = await cr.Container.UpsertItemAsync(
    new Rating
    {
        RatingId = "0",
        ProductId = "1",
        RatingValue = 5,
        User = "kevinpi",
        Review = "Great product!"
    });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.MapGet("averagerating/{productId}", async (string productId, CosmosClient cosmosClient) =>
{
    var container = cosmosClient.GetContainer("ratingsdb", "ratings");

    var feed = container.GetItemLinqQueryable<Rating>(true)
        .Where(r => r.ProductId == productId)
        .ToFeedIterator();

    var ratings = new List<Rating>();
    var sum = 0f;
    var count = 0;
    while (feed.HasMoreResults)
    {
        foreach (var r in await feed.ReadNextAsync())
        {
            sum += r.RatingValue;
            count++;
        }
    }

    var result = count > 0
        ? (sum / count).ToString("0.00", CultureInfo.InvariantCulture) + " / 5"
        : "no ratings";

    return result;
});

app.MapDefaultEndpoints();

app.Run();
