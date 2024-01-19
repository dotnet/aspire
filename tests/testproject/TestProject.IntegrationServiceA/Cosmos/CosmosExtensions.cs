// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;

public static class CosmosExtensions
{
    public static void MapCosmosApi(this WebApplication app)
    {
        app.MapGet("/cosmos/verify", VerifyCosmosAsync);
    }

    private static async Task<IResult> VerifyCosmosAsync(CosmosClient cosmosClient)
    {
        try
        {
            var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync("db")).Database;
            var container = (await db.CreateContainerIfNotExistsAsync("todos", "/id")).Container;

            var id = Guid.NewGuid().ToString();
            var title = "Do some work.";

            var item = await container.CreateItemAsync(new
            {
                id = id,
                title = title
            });

            return item.Resource.id == id ? Results.Ok() : Results.Problem();
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
