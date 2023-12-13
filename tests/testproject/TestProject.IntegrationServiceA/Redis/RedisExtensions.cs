// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StackExchange.Redis;

public static class RedisExtensions
{
    public static void MapRedisApi(this WebApplication app)
    {
        app.MapGet("/redis/verify", VerifyRedisAsync);
    }

    private static async Task<IResult> VerifyRedisAsync(IConnectionMultiplexer cm)
    {
        try
        { 
        var key = "somekey";
        var content = "somecontent";

        var database = cm.GetDatabase();
        await database.StringSetAsync(key, content);
        var data = await database.StringGetAsync(key);

            return data == content ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
