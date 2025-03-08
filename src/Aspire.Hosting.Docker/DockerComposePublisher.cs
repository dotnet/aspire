// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Docker;

/// <summary>
/// TODO
/// </summary>
internal sealed class DockerComposePublisher(
    [ServiceKey]string name,
    IOptionsMonitor<DockerComposePublisherOptions> options,
    ILogger<DockerComposePublisher> logger,
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
                "The '--output-path [path]' option was not specified even though '--publisher docker-compose' argument was used."
            );
        }

        var context = new DockerComposePublishingContext(executionContext, publisherOptions.OutputPath, logger, cancellationToken);

        await context.WriteModel(model).ConfigureAwait(false);

        lifetime.StopApplication();
    }
}
