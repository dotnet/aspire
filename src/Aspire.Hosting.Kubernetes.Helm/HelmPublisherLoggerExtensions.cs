// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes.Helm;

internal static partial class HelmPublisherLoggerExtensions
{
    [LoggerMessage(LogLevel.Warning, "{ResourceName} with type '{ResourceType}' is not supported by this publisher")]
    internal static partial void NotSupportedResourceWarning(this ILogger logger, string resourceName, string resourceType);

    [LoggerMessage(LogLevel.Information, "{Message}")]
    internal static partial void WriteMessage(this ILogger logger, string message);

    [LoggerMessage(LogLevel.Information, "Generating Helm Chart output")]
    internal static partial void StartGeneratingHelmChart(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Successfully generated Helm Chart output in '{OutputPath}'")]
    internal static partial void FinishGeneratingHelmChart(this ILogger logger, string outputPath);
}
