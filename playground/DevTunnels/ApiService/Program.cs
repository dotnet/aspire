// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// HACK: I don't like the naming required to resolve the tunnel endpoint here, need to rethink this.
//       This is the current breakdown:
//       https://_tunnel  .  devtunnel-public-frontend-https
//       \___/   \_____/     \______________/ \______/ \___/
//         |        |               |           |        |
//     [scheme] [endpoint name]     |           |     [target endpoint name]
//                  [tunnel resource name]   [target resource name]
builder.Services.AddHttpClient("tunnel-public", client => client.BaseAddress = new("https://_tunnel.devtunnel-public-frontend-https"));

var app = builder.Build();

app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var http = httpClientFactory.CreateClient("tunnel-public");
    var response = await http.GetAsync("/");
    var contentLength = response.Content.Headers.ContentLength ?? response.Content.ReadAsStringAsync().Result.Length;
    return new
    {
        url = response.RequestMessage?.RequestUri?.ToString(),
        status = response.StatusCode,
        contentLength
    };
});

app.Run();
