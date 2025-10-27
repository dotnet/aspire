// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("frontend", client => client.BaseAddress = new("https://frontend"));

var app = builder.Build();

app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var http = httpClientFactory.CreateClient("frontend");
    var response = await http.GetAsync("/");
    var contentLength = response.Content.Headers.ContentLength ?? (await response.Content.ReadAsStringAsync()).Length;
    return new
    {
        url = response.RequestMessage?.RequestUri?.ToString(),
        status = response.StatusCode,
        contentLength
    };
});

app.Run();
