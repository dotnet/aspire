// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StackExchange.Redis;

public static class RedisExtensions
{
    public static void MapRedisApi(this WebApplication app)
    {
        app.MapPost("/redis/{key}", SetKeyAsync);

        app.MapGet("/redis/{key}", GetKeyAsync);
    }

    private static async Task<IResult> SetKeyAsync(string key, HttpContext context, IConnectionMultiplexer cm)
    {
        using var sr = new StreamReader(context.Request.Body);
        var body = await sr.ReadToEndAsync();

        var database = cm.GetDatabase();
        await database.StringSetAsync(key, body);
        return Results.Ok();
    }

    private static async Task<IResult> GetKeyAsync(string key, IConnectionMultiplexer cm)
    {
        var database = cm.GetDatabase();
        return Results.Content(await database.StringGetAsync(key));
    }
}
