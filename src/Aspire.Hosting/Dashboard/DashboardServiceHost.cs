// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Hosts a gRPC service via <see cref="DashboardService"/> (aka the "Resource Service") that a dashboard can connect to.
/// Configures DI and networking options for the service.
/// </summary>
internal sealed class DashboardServiceHost : IHostedService
{
    /// <summary>
    /// Name of the environment variable that optionally specifies the resource service URL,
    /// which the dashboard will connect to over gRPC.
    /// </summary>
    /// <remarks>
    /// This is primarily intended for cases outside of the local developer environment.
    /// If no value exists for this variable, a port is assigned dynamically.
    /// </remarks>
    private const string DashboardServiceUrlVariableName = "DOTNET_DASHBOARD_GRPC_ENDPOINT_URL";

    private readonly TaskCompletionSource<ImmutableArray<string>> _uris = new();

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
        if (!options.DashboardEnabled ||
            publishingOptions.Value.Publisher == "manifest") // HACK: Manifest publisher check is temporary until DcpHostService is integrated with DcpPublisher.
        {
            _uris.SetCanceled();
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
            // Check env var for URLs to listen on.
            var uris = EnvironmentUtil.GetAddressUris(DashboardServiceUrlVariableName, defaultValue: null);

            string? scheme;

            if (uris is null or { Length: 0 })
            {
                // No URI available from the environment.
                scheme = null;

                // Listen on a random port.
                kestrelOptions.Listen(IPAddress.Loopback, port: 0, ConfigureListen);
            }
            else
            {
                // We have one or more configured URLs to listen on.
                foreach (var uri in uris)
                {
                    scheme = uri.Scheme;

                    if (uri.IsLoopback)
                    {
                        kestrelOptions.ListenLocalhost(uri.Port, ConfigureListen);
                    }
                    else
                    {
                        kestrelOptions.Listen(IPAddress.Parse(uri.Host), uri.Port, ConfigureListen);
                    }
                }
            }

            void ConfigureListen(ListenOptions options)
            {
                // Force HTTP/2 for gRPC, so that it works over non-TLS connections
                // which cannot negotiate between HTTP/1.1 and HTTP/2.
                options.Protocols = HttpProtocols.Http2;

                if (string.Equals(scheme, "https", StringComparison.Ordinal))
                {
                    options.UseHttps();
                }
            }
        }
    }

    /// <summary>
    /// Gets the URIs upon which the resource service is listening.
    /// </summary>
    /// <remarks>
    /// Intended to be used by the app model when launching the dashboard process, populating the
    /// <c>DOTNET_DASHBOARD_GRPC_ENDPOINT_URL</c> environment variable (semicolon delimited).
    /// </remarks>
    public Task<ImmutableArray<string>> GetUrisAsync(CancellationToken cancellationToken = default)
    {
        return _uris.Task.WaitAsync(cancellationToken);
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        if (_app is not null)
        {
            await _app.StartAsync(cancellationToken).ConfigureAwait(false);

            var addressFeature = _app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>();

            if (addressFeature is null)
            {
                _uris.SetException(new InvalidOperationException("Could not obtain IServerAddressesFeature. Dashboard URIs are not available."));
                return;
            }

            _uris.SetResult(addressFeature.Addresses.ToImmutableArray());
        }
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _uris.TrySetCanceled(cancellationToken);

        if (_app is not null)
        {
            await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
