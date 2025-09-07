// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Provides a set of extension methods for interacting with ANSI console
/// functionality within the Kubernetes hosting context.
/// This static class facilitates additional behaviors or utility methods
/// that extend the base ANSI console capabilities.
/// </summary>
internal static partial class DockerComposePublisherLoggerExtensions
{
    [LoggerMessage(LogLevel.Warning, "{ResourceName} with type '{ResourceType}' is not supported by this publisher")]
    internal static partial void NotSupportedResourceWarning(this ILogger logger, string resourceName, string resourceType);

    [LoggerMessage(LogLevel.Information, "{Message}")]
    internal static partial void WriteMessage(this ILogger logger, string message);

    [LoggerMessage(LogLevel.Information, "Generating Compose output")]
    internal static partial void StartGeneratingDockerCompose(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "No resources found in the model.")]
    internal static partial void EmptyModel(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Successfully generated Compose output in '{OutputPath}'")]
    internal static partial void FinishGeneratingDockerCompose(this ILogger logger, string outputPath);

    [LoggerMessage(LogLevel.Warning, "Failed to get container image for resource '{ResourceName}', it will be skipped in the output.")]
    internal static partial void FailedToGetContainerImage(this ILogger logger, string resourceName);

    [LoggerMessage(LogLevel.Warning, "Not in publishing mode. Skipping writing docker-compose.yaml output file.")]
    internal static partial void NotInPublishingMode(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Failed to copy referenced file '{FilePath}' to '{OutputPath}'")]
    internal static partial void FailedToCopyFile(this ILogger logger, string filePath, string outputPath);
}
