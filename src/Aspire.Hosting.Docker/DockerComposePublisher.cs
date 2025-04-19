// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a publisher that generates a Docker Compose 'docker-compose.yaml' file for a distributed application.
/// </summary>
/// <remarks>
/// This publisher is used when deploying distributed applications with Docker Compose. It converts the application's
/// model into Docker Compose artifacts, which are written to the output path specified in the options.
/// </remarks>
internal sealed class DockerComposePublisher(
    [ServiceKey]string name,
    IOptionsMonitor<DockerComposePublisherOptions> options,
    ILogger<DockerComposePublisher> logger,
    DistributedApplicationExecutionContext executionContext,
    IResourceContainerImageBuilder imageBuilder
    ) : IDistributedApplicationPublisher
{
    /// <summary>
    /// Publishes a distributed application model using the Docker Compose publisher implementation.
    /// </summary>
    /// <param name="model">
    /// The distributed application model to be published.
    /// </param>
    /// <param name="cancellationToken">
    /// Propagates notification that the operation should be canceled.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous publishing operation.
    /// </returns>
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var publisherOptions = options.Get(name);

        if (string.IsNullOrEmpty(publisherOptions.OutputPath))
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publisher docker-compose' argument was used."
            );
        }

        var context = new DockerComposePublishingContext(executionContext, publisherOptions, imageBuilder, logger, cancellationToken);

        await context.WriteModelAsync(model).ConfigureAwait(false);
    }
}
