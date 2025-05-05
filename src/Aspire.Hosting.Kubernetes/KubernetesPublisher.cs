// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// The KubernetesPublisher class is responsible for handling the publishing
/// of a distributed application's model as Kubernetes manifests. This class implements
/// the IDistributedApplicationPublisher interface and integrates with
/// Kubernetes-specific publishing logic.
/// </summary>
/// <remarks>
/// This publisher relies on the configuration provided through
/// <see cref="KubernetesPublisherOptions"/>. The publishing operation ensures
/// that the application model is written to the specified output path, which
/// is managed within a Kubernetes publishing context.
/// </remarks>
/// <param name="name">
/// The name of the publisher instance, which is used to retrieve targeted configuration settings.
/// </param>
/// <param name="options">
/// An IOptionsMonitor instance used to monitor and retrieve the publishing options for Kubernetes.
/// </param>
/// <param name="logger">
/// The logger used to capture execution and debugging information during the publishing operation.
/// </param>
/// <param name="executionContext">
/// The execution context representing the distributed application's runtime environment.
/// </param>
/// <exception cref="DistributedApplicationException">
/// Thrown if a required '--output-path' option is not provided for the Kubernetes publisher configuration.
/// </exception>
/// <seealso cref="KubernetesPublisherOptions"/>
/// <seealso cref="KubernetesPublisherExtensions"/>
/// <seealso cref="DistributedApplicationExecutionContext"/>
/// <seealso cref="IDistributedApplicationPublisher"/>
internal sealed class KubernetesPublisher(
    [ServiceKey]string name,
    IOptionsMonitor<KubernetesPublisherOptions> options,
    ILogger<KubernetesPublisher> logger,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationPublisher
{
    /// Asynchronously publishes a distributed application model to a target environment.
    /// <param name="model">
    /// The distributed application model that represents the application's resources and configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, which can be used to cancel the publish operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous publish operation.
    /// </returns>
    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var publisherOptions = options.Get(name);

        if (string.IsNullOrEmpty(publisherOptions.OutputPath))
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publisher kubernetes' argument was used."
            );
        }

        var context = new KubernetesPublishingContext(executionContext, publisherOptions, logger, cancellationToken);

        await context.WriteModelAsync(model).ConfigureAwait(false);
    }
}
