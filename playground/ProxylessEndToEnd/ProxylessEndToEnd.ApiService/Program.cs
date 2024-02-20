// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddKeyedRedis("redis");

var app = builder.Build();

app.MapGet("/", () =>
{
    return Random.Shared.Next();
});

app.MapGet("/redis", ([FromKeyedServices("redis")] IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    db.StringAppend("key", "a");
    return (string?)db.StringGet("key");
});

app.Run();
