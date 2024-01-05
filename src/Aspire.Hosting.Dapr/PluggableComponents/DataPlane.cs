// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class DataPlane : IHostedService
{
    private const string DashboardUrlVariableName = "DAPR_ASPNETCORE_URLS";
    private const string DashboardUrlDefaultValue = "http://localhost:19999";

    private readonly bool _isAllHttps;
    private readonly WebApplication _app;
    private readonly ILogger<DataPlane> _logger;

    public DataPlane(ILogger<DataPlane> logger, StateStore stateStore)
    {
        _logger = logger;
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Error);

        var dashboardUris = GetAddressUris(DashboardUrlVariableName, DashboardUrlDefaultValue);

        if (dashboardUris.FirstOrDefault() is { } reportedDashboardUri)
        {
            // dotnet watch needs the trailing slash removed. See https://github.com/dotnet/sdk/issues/36709
            _logger.LogInformation("Now listening on: {dashboardUri}", reportedDashboardUri.AbsoluteUri.TrimEnd('/'));
        }

        var dashboardHttpsPort = dashboardUris.FirstOrDefault(IsHttps)?.Port;

        _isAllHttps = dashboardHttpsPort is not null;

        builder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            ConfigureListenAddresses(kestrelOptions, dashboardUris);
        });

        if (!builder.Environment.IsDevelopment())
        {
            // This is set up automatically by the DefaultBuilder when IsDevelopment is true
            // But since this gets packaged up and used in another app, we need it to look for
            // static assets on disk as if it were at development time even when it is not
            builder.WebHost.UseStaticWebAssets();
        }

        if (_isAllHttps)
        {
            // Explicitly configure the HTTPS redirect port as we're possibly listening on multiple HTTPS addresses
            // if the dashboard OTLP URL is configured to use HTTPS too
            builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = dashboardHttpsPort);
        }

        // Add services to the container.
        builder.Services.AddSingleton<StateStore>(stateStore);
        
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery();
        builder.Services.AddLocalization();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        _app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!_app.Environment.IsDevelopment())
        {
            _app.UseExceptionHandler("/Error");
        }
        else
        {
            _app.UseSwagger();
            _app.UseSwaggerUI();
        }

        if (_isAllHttps)
        {
            _app.UseHttpsRedirection();
        }

        _app.UseAuthorization();

        _app.UseAntiforgery();

        var v1Group =
            _app.MapGroup("/v1.0")
                .WithName("v1.0");

        var stateStoreGroup =
            v1Group
                .MapGroup("/statestore")
                .WithName("StateStore");

        stateStoreGroup
            .MapGet(
                "/keys",
                () =>
                {
                    return stateStore.GetKeysAsync();
                })
            .WithName("GetStateStoreKeys")
            .WithOpenApi();

        stateStoreGroup
            .MapGet(
                "/keys/{key}",
                async (string key) =>
                {
                    var value = await stateStore.GetKeyAsync(key).ConfigureAwait(false);

                    return value is not null
                            ? Results.Content(value, "application/json")
                            : Results.NotFound(key);
                })
            .WithName("GetStateStoreKey")
            .WithOpenApi();

        stateStoreGroup
            .MapDelete(
                "/keys/{key}",
                async (string key) =>
                {
                    await stateStore.DeleteAsync(key).ConfigureAwait(false);

                    return Results.NoContent();
                })
            .WithName("DeleteStateStoreKey")
            .WithOpenApi();

        stateStoreGroup
            .MapPut(
                "/keys/{key}",
                async (string key, [FromBody] string content) =>
                {
                    await stateStore.SetAsync(key, content).ConfigureAwait(false);

                    return Results.Accepted();
                })
            .WithName("SetStateStoreKey")
            .WithOpenApi();
    }

    private static Uri[] GetAddressUris(string variableName, string defaultValue)
    {
        var urls = Environment.GetEnvironmentVariable(variableName) ?? defaultValue;
        try
        {
            return urls.Split(';').Select(url => new Uri(url)).ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
        }
    }

    private static void ConfigureListenAddresses(KestrelServerOptions kestrelOptions, Uri[] uris, HttpProtocols? httpProtocols = null)
    {
        foreach (var uri in uris)
        {
            if (uri.IsLoopback)
            {
                kestrelOptions.ListenLocalhost(uri.Port, options =>
                {
                    ConfigureListenOptions(options, uri, httpProtocols);
                });
            }
            else
            {
                kestrelOptions.Listen(IPAddress.Parse(uri.Host), uri.Port, options =>
                {
                    ConfigureListenOptions(options, uri, httpProtocols);
                });
            }
        }

        static void ConfigureListenOptions(ListenOptions options, Uri uri, HttpProtocols? httpProtocols)
        {
            if (IsHttps(uri))
            {
                options.UseHttps();
            }
            if (httpProtocols is not null)
            {
                options.Protocols = httpProtocols.Value;
            }
        }
    }

    private static bool IsHttps(Uri uri) => string.Equals(uri.Scheme, "https", StringComparison.Ordinal);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _app.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _app.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
