// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Orchestration;

internal class DistributedApplicationOrchestrator : BackgroundService, IHostedLifecycleService
{
    private readonly ILogger<DistributedApplicationOrchestrator> _logger;
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly IDistributedApplicationEventing _eventing;

    public DistributedApplicationOrchestrator(ILogger<DistributedApplicationOrchestrator> logger, DistributedApplicationExecutionContext executionContext, IDistributedApplicationEventing eventing)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _eventing = eventing ?? throw new ArgumentNullException(nameof(eventing));
        _eventing.Subscribe<ResourceAddedEvent>(OnResourceAddedAsync);
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Distributed application orchestrator started.");
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Distributed application orchestrator starting.");
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Distributed application orchestrator stopped.");
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Distributed application orchestrator stopping.");
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_executionContext.IsPublishMode)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken).ConfigureAwait(false); // Heartbeat delay.
            await _eventing.PublishAsync(new DistributedApplicationHeartbeatEvent(), stoppingToken).ConfigureAwait(false);
        }
    }

    private Task OnResourceAddedAsync(ResourceAddedEvent @event, CancellationToken cancellationToken)
    {
        if (_executionContext.IsPublishMode)
        {
            return Task.CompletedTask;
        }
        // TODO: Add code to resolve the "operator" for this resource and hand it off.

        _logger.LogInformation("Resource added: {Resource} ({ResourceType}).", @event.Resource.Name, @event.Resource.GetType());
        return Task.CompletedTask;
    }
}
