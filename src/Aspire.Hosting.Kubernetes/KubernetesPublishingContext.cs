// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes;

internal class KubernetesPublishingContext(
    DistributedApplicationExecutionContext executionContext,
    KubernetesPublisherOptions publisherOptions,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    internal Task WriteModelAsync(DistributedApplicationModel model)
    {
        _ = logger;
        _ = cancellationToken;

        if (executionContext.IsRunMode)
        {
            return Task.CompletedTask;
        }

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(publisherOptions.OutputPath);

        throw new NotImplementedException("Publishing to Kubernetes is not yet implemented.");
    }
}
