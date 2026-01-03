// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// ATS exports for logging operations.
/// </summary>
internal static class LoggingExports
{
    /// <summary>
    /// Gets a logger for a resource.
    /// </summary>
    [AspireExport("aspire/getLogger@1", Description = "Gets a logger for a resource")]
    public static ILogger GetLogger(ResourceLoggerService loggerService, IResourceBuilder<IResource> resource)
    {
        return loggerService.GetLogger(resource.Resource);
    }

    /// <summary>
    /// Gets a logger by resource name.
    /// </summary>
    [AspireExport("aspire/getLoggerByName@1", Description = "Gets a logger by resource name")]
    public static ILogger GetLoggerByName(ResourceLoggerService loggerService, string resourceName)
    {
        return loggerService.GetLogger(resourceName);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    [AspireExport("aspire/logInformation@1", Description = "Logs an information message")]
    public static void LogInformation(ILogger logger, string message)
    {
        logger.LogInformation("{Message}", message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    [AspireExport("aspire/logWarning@1", Description = "Logs a warning message")]
    public static void LogWarning(ILogger logger, string message)
    {
        logger.LogWarning("{Message}", message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    [AspireExport("aspire/logError@1", Description = "Logs an error message")]
    public static void LogError(ILogger logger, string message)
    {
        logger.LogError("{Message}", message);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    [AspireExport("aspire/logDebug@1", Description = "Logs a debug message")]
    public static void LogDebug(ILogger logger, string message)
    {
        logger.LogDebug("{Message}", message);
    }

    /// <summary>
    /// Logs a message with a specified log level.
    /// </summary>
    [AspireExport("aspire/log@1", Description = "Logs a message with specified level")]
    public static void Log(ILogger logger, string level, string message)
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
    [AspireExport("aspire/completeLog@1", Description = "Completes the log stream for a resource")]
    public static void CompleteLog(ResourceLoggerService loggerService, IResourceBuilder<IResource> resource)
    {
        loggerService.Complete(resource.Resource);
    }

    /// <summary>
    /// Completes the log stream by resource name.
    /// </summary>
    [AspireExport("aspire/completeLogByName@1", Description = "Completes the log stream by resource name")]
    public static void CompleteLogByName(ResourceLoggerService loggerService, string resourceName)
    {
        loggerService.Complete(resourceName);
    }
}
