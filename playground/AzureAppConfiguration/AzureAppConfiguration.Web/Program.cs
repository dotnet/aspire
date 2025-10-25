// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.AppConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureAppConfiguration("appconfig", configureOptions: options =>
{
    options.ConfigureRefresh(refresh =>
    {
        refresh.RegisterAll();
        refresh.SetRefreshInterval(TimeSpan.FromSeconds(10));
    });
});

builder.AddAzureAppConfigurationClient("appconfig");

var app = builder.Build();

app.MapGet("/message", (IConfiguration config) =>
{
    var message = config["Message"];
    return new { Message = message };
});

app.MapGet("/setmessage", async (string message, ConfigurationClient client) =>
{
    try
    {
        await client.SetConfigurationSettingAsync("Message", message);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Use Azure App Configuration middleware for dynamic configuration refresh.
app.UseAzureAppConfiguration();

app.Run();
