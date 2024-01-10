// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Hosts a gRPC service (via <see cref="DashboardService"/>) that a dashboard can connect to.
/// Configures DI and networking options for the service.
/// </summary>
internal sealed class DashboardServiceHost : IHostedService
{
    private const string DashboardServiceUrlVariableName = "DOTNET_DASHBOARD_GRPC_ENDPOINT_URL";
    private const string DashboardServiceUrlDefaultValue = "http://localhost:18999";

    /// <summary>
    /// <see langword="null"/> if <see cref="DistributedApplicationOptions.DashboardEnabled"/> is <see langword="false"/>.
    /// </summary>
    private readonly WebApplication? _app;

    public DashboardServiceHost(
        DistributedApplicationOptions options,
        DistributedApplicationModel applicationModel,
        KubernetesService kubernetesService,
        IOptions<PublishingOptions> publishingOptions,
        ILoggerFactory loggerFactory,
        IConfigureOptions<LoggerFilterOptions> loggerOptions)
    {
        if (!options.DashboardEnabled)
        {
            return;
        }
        else if (publishingOptions.Value.Publisher == "manifest")
        {
            // HACK: Manifest publisher check is temporary until DcpHostService is integrated with DcpPublisher.
            return;
        }

        var builder = WebApplication.CreateBuilder();

        // Logging
        builder.Services.AddSingleton(loggerFactory);
        builder.Services.AddSingleton(loggerOptions);
        builder.Services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        builder.Services.AddGrpc();
        builder.Services.AddSingleton(applicationModel);
        builder.Services.AddSingleton(kubernetesService);
        builder.Services.AddSingleton<DashboardServiceData>();

        builder.WebHost.ConfigureKestrel(ConfigureKestrel);

        _app = builder.Build();

        _app.MapGrpcService<DashboardService>();

        return;

        static void ConfigureKestrel(KestrelServerOptions kestrelOptions)
        {
            var uris = GetAddressUris(DashboardServiceUrlVariableName, DashboardServiceUrlDefaultValue);

            foreach (var uri in uris)
            {
                if (uri.IsLoopback)
                {
                    kestrelOptions.ListenLocalhost(uri.Port, ConfigureListen);
                }
                else
                {
                    kestrelOptions.Listen(IPAddress.Parse(uri.Host), uri.Port, ConfigureListen);
                }

                void ConfigureListen(ListenOptions options)
                {
                    // Force HTTP/2 for gRPC, so that it works over non-TLS connections
                    // which cannot negotiate between HTTP/1.1 and HTTP/2.
                    options.Protocols = HttpProtocols.Http2;

                    if (string.Equals(uri.Scheme, "https", StringComparison.Ordinal))
                    {
                        options.UseHttps();
                    }
                }
            }

            static Uri[] GetAddressUris(string variableName, string defaultValue)
            {
                try
                {
                    var urls = Environment.GetEnvironmentVariable(variableName) ?? defaultValue;

                    return urls.Split(';').Select(url => new Uri(url)).ToArray();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
                }
            }
        }
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        if (_app is not null)
        {
            await _app.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        if (_app is not null)
        {
            await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
