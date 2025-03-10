// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Kubernetes.Helm;

/// <summary>
/// HelmPublisher is responsible for publishing a distributed application model in the form of Helm charts.
/// Utilizing Kubernetes and Helm, it processes the application model and generates the required resources for deployment, ensuring
/// the output adheres to the specified configuration provided through HelmPublisherOptions.
/// </summary>
/// <remarks>
/// The HelmPublisher is implemented as an internal sealed class and is invoked as part of a distributed application
/// publishing process. It complies with the IDistributedApplicationPublisher interface and interacts with various dependencies,
/// such as logging, application lifetime, and execution context, to enable robust Helm-based deployments.
/// </remarks>
/// <param name="name">
/// The unique name used as a service key for retrieving the corresponding configuration options.
/// </param>
/// <param name="options">
/// Monitored configuration options specific to the Helm publisher, which are evaluated and utilized during the publish operation.
/// </param>
/// <param name="logger">
/// Logger instance for capturing debug, info, warnings, or error logs during the publish workflow.
/// </param>
/// <param name="executionContext">
/// Provides contextual information about the application, allowing coordination for distributed application operations.
/// </param>
/// <exception cref="DistributedApplicationException">
/// Thrown when mandatory publishing options, such as the output path, are missing from the supplied configuration.
/// </exception>
/// <remarks>
/// The publishing process concludes by stopping the application via the IHostApplicationLifetime interface.
/// </remarks>
internal sealed class HelmPublisher(
    [ServiceKey]string name,
    IOptionsMonitor<HelmPublisherOptions> options,
    ILogger<HelmPublisher> logger,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationPublisher
{
    /// <summary>
    /// Publishes the distributed application model asynchronously.
    /// </summary>
    /// <param name="model">The distributed application model to publish.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var publisherOptions = options.Get(name);

        if (string.IsNullOrEmpty(publisherOptions.OutputPath))
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publisher kubernetes' argument was used."
            );
        }

        var context = new HelmPublishingContext(executionContext, publisherOptions.OutputPath, logger, cancellationToken);

        await context.WriteModel(model).ConfigureAwait(false);
    }
}
