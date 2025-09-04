// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClientBuilder("redis")
    .WithAzureAuthentication()
    .WithDistributedCache();
builder.AddKeyedRedisClient("garnet");
builder.AddKeyedRedisClient("valkey");

var app = builder.Build();

app.MapGet("/redis/ping", async (IConnectionMultiplexer connection, IDistributedCache cache) =>
{
    return $"{cache.GetType()} {await connection.GetDatabase().PingAsync()}";
});

app.MapGet("/redis/set", async (IConnectionMultiplexer connection, IDistributedCache cache) =>
{
    await cache.SetStringAsync("MyKey", $"{DateTime.Now}");
    return await connection.GetDatabase().StringSetAsync("Key", $"{DateTime.Now}");
    
});

app.MapGet("/redis/get", async (IConnectionMultiplexer connection, IDistributedCache cache) =>
{
    var c = cache.GetString("MyKey");
    var redisValue = await connection.GetDatabase().StringGetAsync("Key");
    var d = redisValue.HasValue ? redisValue.ToString() : "(null)";
    return $"{c} {d}";
});

app.MapGet("/garnet/ping", async ([FromKeyedServices("garnet")] IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().PingAsync();
});

app.MapGet("/garnet/set", async ([FromKeyedServices("garnet")] IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().StringSetAsync("Key", $"{DateTime.Now}");
});

app.MapGet("/garnet/get", async ([FromKeyedServices("garnet")] IConnectionMultiplexer connection) =>
{
    var redisValue = await connection.GetDatabase().StringGetAsync("Key");
    return redisValue.HasValue ? redisValue.ToString() : "(null)";
});

app.MapGet("/valkey/ping", async ([FromKeyedServices("valkey")] IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().PingAsync();
});

app.MapGet("/valkey/set", async ([FromKeyedServices("valkey")] IConnectionMultiplexer connection) =>
{
    return await connection.GetDatabase().StringSetAsync("Key", $"{DateTime.Now}");
});

app.MapGet("/valkey/get", async ([FromKeyedServices("valkey")] IConnectionMultiplexer connection) =>
{
    var redisValue = await connection.GetDatabase().StringGetAsync("Key");
    return redisValue.HasValue ? redisValue.ToString() : "(null)";
});

app.MapDefaultEndpoints();

app.Run();
