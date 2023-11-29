// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Orleans.Client;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .UseAspireOrleansClient();

var app = builder.Build();

app.MapGet("/counter/{grainId}", async (IClusterClient client, string grainId) =>
{
    var grain = client.GetGrain<ICounterGrain>(grainId);
    return await grain.Get();
});

app.MapPost("/counter/{grainId}", async (IClusterClient client, string grainId) =>
{
    var grain = client.GetGrain<ICounterGrain>(grainId);
    return await grain.Increment();
});

app.UseFileServer();

app.Run();
