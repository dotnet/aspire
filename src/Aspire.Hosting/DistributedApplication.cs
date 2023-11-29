// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting;

internal enum DockerHealthCheckFailures : int
{
    /// <summary>
    /// Represents the error code for when the invocation of Docker CLI test command didn't finish within expected time period.
    /// </summary>
    Unresponsive = 125,

    /// <summary>
    /// Represents the error code for when the Docker CLI test command returned an error exit code.
    /// </summary>
    Unhealthy = 126,

    /// <summary>
    /// Represents the exit code indicating that a prerequisite for running the application are missing.
    /// </summary>
    PrerequisiteMissing = 127
}

internal enum DcpVersionCheckFailures: int
{
    /// <summary>
    /// Represents the exit code indicating that the version of DCP is too low or too high.
    /// </summary>
    DcpVersionIncompatible = 128,
}

/// <summary>
/// Represents a distributed application that implements the <see cref="IHost"/> and <see cref="IAsyncDisposable"/> interfaces.
/// </summary>
[DebuggerDisplay("{_host}")]
public class DistributedApplication : IHost, IAsyncDisposable
{
    private readonly IHost _host;
    private readonly string[] _args;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedApplication"/> class.
    /// </summary>
    /// <param name="host">The <see cref="IHost"/> instance.</param>
    /// <param name="args">The command-line arguments.</param>
    public DistributedApplication(IHost host, string[] args)
    {
        _host = host;
        _args = args;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IDistributedApplicationBuilder"/> interface.
    /// </summary>
    /// <returns>A new instance of the <see cref="IDistributedApplicationBuilder"/> interface.</returns>
    public static IDistributedApplicationBuilder CreateBuilder() => CreateBuilder([]);

    /// <summary>
    /// Creates a new instance of <see cref="IDistributedApplicationBuilder"/> with the specified command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to use when building the distributed application.</param>
    /// <returns>A new instance of <see cref="IDistributedApplicationBuilder"/>.</returns>
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = new DistributedApplicationBuilder(new DistributedApplicationOptions() { Args = args });
        return builder;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IDistributedApplicationBuilder"/> interface with the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The <see cref="DistributedApplicationOptions"/> to use for configuring the builder.</param>
    /// <returns>A new instance of the <see cref="IDistributedApplicationBuilder"/> interface.</returns>
    public static IDistributedApplicationBuilder CreateBuilder(DistributedApplicationOptions options)
    {
        var builder = new DistributedApplicationBuilder(options);
        return builder;
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance configured for the application.
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Disposes the distributed application by disposing the <see cref="IHost"/>.
    /// </summary>
    public void Dispose()
    {
        _host.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the distributed application by disposing the <see cref="IHost"/>.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)_host).DisposeAsync();
    }

    /// <inheritdoc cref="IHost.StartAsync" />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBeforeStartHooksAsync(cancellationToken).ConfigureAwait(false);
        await _host.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IHost.StopAsync" />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _host.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private void SuppressLifetimeLogsDuringManifestPublishing()
    {
        var config = (IConfigurationRoot)_host.Services.GetRequiredService<IConfiguration>();
        var options = _host.Services.GetRequiredService<IOptions<PublishingOptions>>();

        if (options.Value?.Publisher != "manifest")
        {
            // If we aren't doing manifest publishing we want the logs
            // to be produced as normal.
            return;
        }

        var hostingLifetimeLoggingLevelSection = config.GetSection("Logging:LogLevel:Microsoft.Hosting.Lifetime");
        hostingLifetimeLoggingLevelSection.Value = "Warning";

        config.Reload();
    }

    /// <inheritdoc cref="HostingAbstractionsHostExtensions.RunAsync" />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        SuppressLifetimeLogsDuringManifestPublishing();
        await ExecuteBeforeStartHooksAsync(cancellationToken).ConfigureAwait(false);
        await _host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the distributed application and only completes when the token is triggered or shutdown is triggered.
    /// </summary>
    public void Run()
    {
        RunAsync().Wait();
    }

    private async Task ExecuteBeforeStartHooksAsync(CancellationToken cancellationToken)
    {
        AspireEventSource.Instance.AppBeforeStartHooksStart();

        try
        {
            var lifecycleHooks = _host.Services.GetServices<IDistributedApplicationLifecycleHook>();
            var appModel = _host.Services.GetRequiredService<DistributedApplicationModel>();

            foreach (var lifecycleHook in lifecycleHooks)
            {
                await lifecycleHook.BeforeStartAsync(appModel, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            AspireEventSource.Instance.AppBeforeStartHooksStop();
        }
    }

    Task IHost.StartAsync(CancellationToken cancellationToken) => StartAsync(cancellationToken);

    Task IHost.StopAsync(CancellationToken cancellationToken) => StopAsync(cancellationToken);
}
