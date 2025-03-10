// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Kubernetes.Kustomize;

/// <summary>
/// The KustomizePublisher class is responsible for publishing a distributed application model
/// using Kustomize to generate Kubernetes artifacts. This implementation of
/// <see cref="IDistributedApplicationPublisher"/> interacts with the application execution context
/// and configuration options to produce relevant Kubernetes manifest files and then signals
/// the application to stop after publishing.
/// </summary>
/// <remarks>
/// This class is a sealed, internal implementation and can be used to handle publishing behavior
/// via the KustomizePublisherOptions configuration. The publisher writes data to the specified
/// output path and ensures resource models are properly formatted and stored.
/// </remarks>
/// <param name="name">
/// The unique key associated with this publisher instance, typically used to retrieve configured options.
/// </param>
/// <param name="options">
/// Monitor for the dynamic retrieval of configured options specific to this publisher.
/// </param>
/// <param name="logger">
/// Logger instance to provide diagnostics and logging capabilities.
/// </param>
/// <param name="executionContext">
/// Contextual information describing the distributed application execution state.
/// </param>
/// <example>
/// This class does not provide direct user-facing access. To use the functionality, register through
/// dependency injection or the provided extension methods.
/// </example>
internal sealed class KustomizePublisher(
    [ServiceKey]string name,
    IOptionsMonitor<KustomizePublisherOptions> options,
    ILogger<KustomizePublisher> logger,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationPublisher
{
    /// <summary>
    /// Publishes the provided distributed application model asynchronously using the Kustomize configuration approach.
    /// </summary>
    /// <param name="model">The distributed application model containing the resources to be published.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation to cancel.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var publisherOptions = options.Get(name);

        if (string.IsNullOrEmpty(publisherOptions.OutputPath))
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publisher kubernetes' argument was used."
            );
        }

        var context = new KustomizePublishingContext(executionContext, publisherOptions, logger, cancellationToken);

        await context.WriteModel(model).ConfigureAwait(false);
    }
}
