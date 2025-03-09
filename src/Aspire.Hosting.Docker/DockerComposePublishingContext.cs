// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Contextual information used for manifest publishing during this execution of the AppHost as docker compose output format.
/// </summary>
/// <param name="executionContext">Global contextual information for this invocation of the AppHost.</param>
/// <param name="outputPath">Output path for assets generated via this invocation of the AppHost.</param>
/// <param name="logger">The current publisher logger instance.</param>
/// <param name="cancellationToken">Cancellation token for this operation.</param>
internal sealed class DockerComposePublishingContext(DistributedApplicationExecutionContext executionContext, string outputPath, ILogger logger, CancellationToken cancellationToken = default)
{
    internal Task WriteModel(DistributedApplicationModel model)
    {
        // TODO will be USING THESE
        _ = executionContext;
        _ = cancellationToken;

        logger.StartGeneratingDockerCompose();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(outputPath);

        Directory.CreateDirectory(outputPath);

        logger.FinishGeneratingDockerCompose(outputPath);

        return Task.CompletedTask;
    }
}
