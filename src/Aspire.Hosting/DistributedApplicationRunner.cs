// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal sealed class DistributedApplicationRunner(DistributedApplicationExecutionContext executionContext, DistributedApplicationModel model, IServiceProvider serviceProvider) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (executionContext.IsPublishMode)
        {
            return serviceProvider.GetRequiredKeyedService<IDistributedApplicationPublisher>(executionContext.PublisherName).PublishAsync(model, stoppingToken);
        }

        return Task.CompletedTask;
    }
}
