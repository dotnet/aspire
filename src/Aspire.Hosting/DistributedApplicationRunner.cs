// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(DistributedApplicationExecutionContext executionContext, DistributedApplicationModel model, IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var publisher = executionContext.Operation switch
        {
            DistributedApplicationOperation.Publish => serviceProvider.GetRequiredKeyedService<IDistributedApplicationPublisher>("manifest"),
            _ => serviceProvider.GetRequiredKeyedService<IDistributedApplicationPublisher>("dcp")
        };

        await publisher.PublishAsync(model, stoppingToken).ConfigureAwait(false);
    }
}
