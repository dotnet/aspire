// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Elastic.Clients.Elasticsearch;

public static class ElasticsearchExtensions
{
    public static void MapElasticsearchApi(this WebApplication app)
    {
        app.MapGet("/elasticsearch/verify", VerifyElasticsearchAsync);
    }

    private static async Task<IResult> VerifyElasticsearchAsync(ElasticsearchClient client)
    {
        try
        {
            var infoResponse = await client.InfoAsync();
            if (infoResponse.ApiCallDetails.HttpStatusCode == 200)
            {
                return Results.Ok($"Success! Elasticsearch version: {infoResponse.Version}");
            }
            return Results.Problem(infoResponse.ApiCallDetails.DebugInformation);
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
