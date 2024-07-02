// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("redis");

var app = builder.Build();

app.MapGet("/ping", async (IConnectionMultiplexer connection) =>
{
   return await connection.GetDatabase().PingAsync();
});

app.MapGet("/set", async (IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().StringSetAsync("Key",$"{DateTime.Now}");
});

app.MapGet("/get", async (IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().StringGetAsync("Key");
});

app.Run();
