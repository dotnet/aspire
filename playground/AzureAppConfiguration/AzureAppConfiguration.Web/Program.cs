// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureAppConfiguration("appconfig", configureOptions: options =>
{
    options.ConfigureRefresh(refresh =>
    {
        refresh.RegisterAll();
        refresh.SetRefreshInterval(TimeSpan.FromSeconds(10));
    });
});

var app = builder.Build();

app.MapGet("/message", (IConfiguration config) =>
{
    var message = config["Message"];
    return new { Message = message };
});

// Use Azure App Configuration middleware for dynamic configuration refresh.
app.UseAzureAppConfiguration();

app.Run();
