// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddHttpClient("gateway-client", client =>
{
    client.BaseAddress = new Uri("https+http://gateway");
});

var app = builder.Build();

app.UseFileServer();

app.MapGet("/api/weatherforecast", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("gateway-client");
    var response = await client.GetAsync("/api/weatherforecast");
    response.EnsureSuccessStatusCode();
    return Results.Content(await response.Content.ReadAsStringAsync(), "application/json");
});

app.Run();
