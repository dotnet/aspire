// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes;

internal static partial class KubernetesPublisherLoggerExtensions
{
    [LoggerMessage(LogLevel.Warning, "{ResourceName} with type '{ResourceType}' is not supported by this publisher")]
    internal static partial void NotSupportedResourceWarning(this ILogger logger, string resourceName, string resourceType);

    [LoggerMessage(LogLevel.Information, "{Message}")]
    internal static partial void WriteMessage(this ILogger logger, string message);

    [LoggerMessage(LogLevel.Information, "Generating Kubernetes output")]
    internal static partial void StartGeneratingKubernetes(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "No resources found in the model.")]
    internal static partial void EmptyModel(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Successfully generated Kubernetes output in '{OutputPath}'")]
    internal static partial void FinishGeneratingKubernetes(this ILogger logger, string outputPath);

    [LoggerMessage(LogLevel.Warning, "Failed to get container image for resource '{ResourceName}', it will be skipped in the output.")]
    internal static partial void FailedToGetContainerImage(this ILogger logger, string resourceName);

    [LoggerMessage(LogLevel.Warning, "Not in publishing mode. Skipping writing kubernetes manifests.")]
    internal static partial void NotInPublishingMode(this ILogger logger);
}
