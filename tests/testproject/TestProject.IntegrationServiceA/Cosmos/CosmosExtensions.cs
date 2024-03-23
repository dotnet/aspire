// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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
        StringBuilder log = new();
        try
        {
            TimeSpan totalTimeout = TimeSpan.FromSeconds(90);
            // Stopwatch sw = new();
            var policy = Policy
                .Handle<HttpRequestException>()
                // retry 60 times with a 1 second delay between retries
                .WaitAndRetryAsync(20, retryAttempt => TimeSpan.FromSeconds(1));
                    // 10,
                    // retryAttempt => TimeSpan.FromSeconds(1));
                    // retryAttempt => (totalTimeout.TotalMilliseconds - sw.ElapsedMilliseconds) > 5000 ? TimeSpan.FromSeconds(1) : TimeSpan.Zero,
                    // onRetry: (exception, sleepDuration)  =>
                    //     log.AppendLine(CultureInfo.InvariantCulture, $"Retry due to {exception.Message}"));

            // sw.Start();
            var db = await policy.ExecuteAsync(async () => (
                        await cosmosClient.CreateDatabaseIfNotExistsAsync("db")).Database);

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
            return Results.Problem(e.ToString() + $" Log: {log}");
        }
    }
}
