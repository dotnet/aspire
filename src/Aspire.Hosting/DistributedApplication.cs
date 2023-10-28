// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public enum DockerHealthCheckFailures : int
{
    Unresponsive = 125, // Invocation of Docker CLI test command did not finish within expected time period.
    Unhealthy = 126,    // The Docker CLI test command returned an error exit code.
    PrerequisiteMissing = 127 // We could not invoke Docker CLI, Docker may be missing from the machine.
}

[DebuggerDisplay("{_host}")]
public class DistributedApplication : IHost, IAsyncDisposable
{
    private readonly IHost _host;
    private readonly string[] _args;

    public DistributedApplication(IHost host, string[] args)
    {
        _host = host;
        _args = args;
    }

    public static IDistributedApplicationBuilder CreateBuilder() => CreateBuilder([]);

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = new DistributedApplicationBuilder(new DistributedApplicationOptions() { Args = args });
        return builder;
    }

    public static IDistributedApplicationBuilder CreateBuilder(DistributedApplicationOptions options)
    {
        var builder = new DistributedApplicationBuilder(options);
        return builder;
    }

    public IServiceProvider Services => _host.Services;

    public void Dispose()
    {
        _host.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)_host).DisposeAsync();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBeforeStartHooksAsync(cancellationToken).ConfigureAwait(false);
        await _host.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _host.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteBeforeStartHooksAsync(cancellationToken).ConfigureAwait(false);
        await _host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

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

