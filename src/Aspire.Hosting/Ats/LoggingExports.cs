// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Ats;

/// <summary>
/// ATS exports for logging operations.
/// </summary>
internal static class LoggingExports
{
    /// <summary>
    /// Gets a logger for a resource.
    /// </summary>
    [AspireExport("getLogger", Description = "Gets a logger for a resource")]
    public static ILogger GetLogger(ResourceLoggerService loggerService, IResourceBuilder<IResource> resource)
    {
        return loggerService.GetLogger(resource.Resource);
    }

    /// <summary>
    /// Gets a logger by resource name.
    /// </summary>
    [AspireExport("getLoggerByName", Description = "Gets a logger by resource name")]
    public static ILogger GetLoggerByName(ResourceLoggerService loggerService, string resourceName)
    {
        return loggerService.GetLogger(resourceName);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    [AspireExport("logInformation", Description = "Logs an information message")]
    public static void LogInformation(this ILogger logger, string message)
    {
        logger.LogInformation("{Message}", message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    [AspireExport("logWarning", Description = "Logs a warning message")]
    public static void LogWarning(this ILogger logger, string message)
    {
        logger.LogWarning("{Message}", message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    [AspireExport("logError", Description = "Logs an error message")]
    public static void LogError(this ILogger logger, string message)
    {
        logger.LogError("{Message}", message);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    [AspireExport("logDebug", Description = "Logs a debug message")]
    public static void LogDebug(this ILogger logger, string message)
    {
        logger.LogDebug("{Message}", message);
    }

    /// <summary>
    /// Logs a message with a specified log level.
    /// </summary>
    [AspireExport("log", Description = "Logs a message with specified level")]
    public static void Log(this ILogger logger, string level, string message)
    {
        var logLevel = level.ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" or "info" => LogLevel.Information,
            "warning" or "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        logger.Log(logLevel, "{Message}", message);
    }

    /// <summary>
    /// Completes the log stream for a resource.
    /// </summary>
    [AspireExport("completeLog", Description = "Completes the log stream for a resource")]
    public static void CompleteLog(ResourceLoggerService loggerService, IResourceBuilder<IResource> resource)
    {
        loggerService.Complete(resource.Resource);
    }

    /// <summary>
    /// Completes the log stream by resource name.
    /// </summary>
    [AspireExport("completeLogByName", Description = "Completes the log stream by resource name")]
    public static void CompleteLogByName(ResourceLoggerService loggerService, string resourceName)
    {
        loggerService.Complete(resourceName);
    }
}
