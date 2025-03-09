// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes.Helm;

/// <summary>
/// Represents the context for publishing a distributed application model as a Helm Chart.
/// This class is responsible for managing the process of writing the application model
/// to a specified output path during the publishing process.
/// </summary>
internal sealed class HelmPublishingContext(DistributedApplicationExecutionContext executionContext, string outputPath, ILogger logger, CancellationToken cancellationToken = default)
{
    internal Task WriteModel(DistributedApplicationModel model)
    {
        // TODO will be USING THESE
        _ = executionContext;
        _ = cancellationToken;

        logger.StartGeneratingHelmChart();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(outputPath);

        Directory.CreateDirectory(outputPath);

        logger.FinishGeneratingHelmChart(outputPath);

        return Task.CompletedTask;
    }
}
