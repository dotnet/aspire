// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapr.PluggableComponents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class DaprPluggableComponents : IDisposable
{
    private readonly DaprPluggableComponentsApplication _app = DaprPluggableComponentsApplication.Create();
    private readonly ILogger<DaprPluggableComponents> _logger;

    public DaprPluggableComponents(ILogger<DaprPluggableComponents> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(
        string socketFolder,
        string socketName)
    {
        _logger.LogInformation("Starting pluggable components...");

        _app.Services.AddSingleton<StateStore>();
        _app.Services.AddHostedService<DataPlane>();

        _app.RegisterService(
            new DaprPluggableComponentsServiceOptions(socketName)
            {
                SocketFolder = socketFolder
            },
            serviceBuilder =>
            {
                serviceBuilder.RegisterPubSub(
                    context =>
                    {
                        _logger.LogInformation("Creating Aspire pub sub for instance '{InstanceId}' on socket '{SocketPath}'...", context.InstanceId, context.SocketPath);

                        return new MemoryPubSub(context.ServiceProvider.GetRequiredService<ILogger<MemoryPubSub>>());
                    });

                serviceBuilder.RegisterStateStore(
                    context =>
                    {
                        _logger.LogInformation("Creating Aspire state store for instance '{InstanceId}' on socket '{SocketPath}'...", context.InstanceId, context.SocketPath);

                        return new MemoryStateStore(
                            context.ServiceProvider.GetRequiredService<ILogger<MemoryStateStore>>(),
                            context.ServiceProvider.GetRequiredService<StateStore>());
                    });
            });

        await _app.StartAsync().ConfigureAwait(false);

        _logger.LogInformation("Pluggable components started.");
    }

    public void Dispose()
    {
        _logger.LogInformation("Stopping pluggable components...");

        // TODO: Don't block on stop.
        _app.StopAsync().Wait();

        _logger.LogInformation("Pluggable components stopped.");
    }
}