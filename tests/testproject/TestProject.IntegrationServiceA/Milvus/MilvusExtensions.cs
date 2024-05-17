// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Milvus.Client;

public static class MilvusExtensions
{
    public static void MapMilvusApi(this WebApplication app)
    {
        app.MapGet("/milvus/verify", VerifyMilvusAsync);
    }

    private static async Task<IResult> VerifyMilvusAsync(MilvusClient client)
    {
        try
        {
            var versionString = await client.GetVersionAsync();
            return Results.Ok($"Success! Milvus version: {versionString}");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
