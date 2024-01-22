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
using System.Diagnostics.CodeAnalysis;

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
        if (!TryGetDashboardServiceUriFromEnviroment(out var dashboardServiceUri))
        {
            dashboardServiceUri = new Uri(DashboardServiceUrlDefaultValue);
        }

        DashboardServiceUri = dashboardServiceUri;

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

        void ConfigureKestrel(KestrelServerOptions kestrelOptions)
        {

            if (DashboardServiceUri.IsLoopback)
            {
                kestrelOptions.ListenLocalhost(dashboardServiceUri.Port, ConfigureListen);
            }
            else
            {
                kestrelOptions.Listen(IPAddress.Parse(DashboardServiceUri.Host), DashboardServiceUri.Port, ConfigureListen);
            }

            void ConfigureListen(ListenOptions options)
            {
                // Force HTTP/2 for gRPC, so that it works over non-TLS connections
                // which cannot negotiate between HTTP/1.1 and HTTP/2.
                options.Protocols = HttpProtocols.Http2;

                if (string.Equals(DashboardServiceUri.Scheme, "https", StringComparison.Ordinal))
                {
                    options.UseHttps();
                }
            }
        }
    }

    public Uri? DashboardServiceUri { get; init; }

    private static bool TryGetDashboardServiceUriFromEnviroment([NotNullWhen(true)]out Uri? uri)
    {
        if (Environment.GetEnvironmentVariable(DashboardServiceUrlVariableName) is not { } value)
        {
            uri = null;
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidateUri))
        {
            uri = null;
            return false;
        }

        if (!StringComparer.InvariantCultureIgnoreCase.Equals(candidateUri.Scheme, "https"))
        {
            uri = null;
            return false;
        }

        if (candidateUri.Host.ToLowerInvariant() != "localhost")
        {
            uri = null;
            return false;
        }

        uri = candidateUri;
        return true;
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
