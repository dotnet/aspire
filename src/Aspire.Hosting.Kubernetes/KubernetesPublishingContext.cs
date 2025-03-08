// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Generators;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Contextual information used for manifest publishing during this execution of the AppHost as kubernetes assets.
/// </summary>
/// <param name="executionContext">Global contextual information for this invocation of the AppHost.</param>
/// <param name="outputPath">Output path for assets generated via this invocation of the AppHost.</param>
/// <param name="logger">The current publisher logger instance.</param>
/// <param name="cancellationToken">Cancellation token for this operation.</param>
internal sealed class KubernetesPublishingContext(DistributedApplicationExecutionContext executionContext, string outputPath, ILogger logger, CancellationToken cancellationToken = default)
{
    internal Task WriteModel(DistributedApplicationModel model, string outputType) =>
        outputType switch
        {
            KubernetesPublisherOutputType.Helm => new HelmOutputGenerator(model, executionContext, outputPath, logger, cancellationToken).WriteManifests(),
            KubernetesPublisherOutputType.Kustomize => new KustomizeGenerator(model, executionContext, outputPath, logger, cancellationToken).WriteManifests(),
            _ => throw new DistributedApplicationException(KubernetesPublisherOutputType.InvalidOutputTypeMessage(outputType)),
        };
}
