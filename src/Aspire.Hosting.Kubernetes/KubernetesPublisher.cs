// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// TODO
/// </summary>
internal sealed class KubernetesPublisher(
    [ServiceKey]string name,
    IOptionsMonitor<KubernetesPublisherOptions> options,
    ILogger<KubernetesPublisher> logger,
    IHostApplicationLifetime lifetime,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationPublisher
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var publisherOptions = options.Get(name);

        if (string.IsNullOrEmpty(publisherOptions.OutputPath))
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publisher kubernetes' argument was used."
            );
        }

        var context = new KubernetesPublishingContext(executionContext, publisherOptions.OutputPath, logger, cancellationToken);

        await context.WriteModel(model, publisherOptions.OutputType).ConfigureAwait(false);

        lifetime.StopApplication();
    }
}
