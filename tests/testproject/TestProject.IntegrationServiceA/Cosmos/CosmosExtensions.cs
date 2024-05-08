// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.TestProject;
using Microsoft.Azure.Cosmos;
using Polly;

public static class CosmosExtensions
{
    public static void MapCosmosApi(this WebApplication app)
    {
        app.MapGet("/cosmos/verify", VerifyCosmosAsync);
    }

    private static async Task<IResult> VerifyCosmosAsync(CosmosClient cosmosClient)
    {
        StringBuilder errorMessageBuilder = new();
        try
        {
            ResiliencePipeline pipeline = ResilienceUtils.GetDefaultResiliencePipelineBuilder<HttpRequestException>(args =>
            {
                errorMessageBuilder.AppendLine($"{Environment.NewLine}Service retry #{args.AttemptNumber} due to {args.Outcome.Exception}");
                return ValueTask.CompletedTask;
            }).Build();

            var db = await pipeline.ExecuteAsync(
                async token => (await cosmosClient.CreateDatabaseIfNotExistsAsync("db", cancellationToken: token)).Database);

            var container = (await db.CreateContainerIfNotExistsAsync("todos", "/id")).Container;

            var id = Guid.NewGuid().ToString();
            var title = "Do some work.";

            var item = await container.CreateItemAsync(new
            {
                id,
                title
            });

            return item.Resource.id == id ? Results.Ok() : Results.Problem();
        }
        catch (Exception e)
        {
            return Results.Problem($"Error: {e}{Environment.NewLine}** Previous retries: {errorMessageBuilder}");
        }
    }
}
