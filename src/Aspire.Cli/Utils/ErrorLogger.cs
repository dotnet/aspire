// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Cli.Utils;

/// <summary>
/// Service for logging errors and exceptions to files.
/// </summary>
internal interface IErrorLogger
{
    /// <summary>
    /// Logs an exception with full details to a log file.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="commandContext">Optional context about the command being executed.</param>
    /// <returns>The path to the log file where the error was written.</returns>
    string LogError(Exception exception, string? commandContext = null);

    /// <summary>
    /// Logs an error message with details to a log file.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional additional details.</param>
    /// <param name="commandContext">Optional context about the command being executed.</param>
    /// <returns>The path to the log file where the error was written.</returns>
    string LogError(string message, string? details = null, string? commandContext = null);

    /// <summary>
    /// Gets the directory where error logs are stored.
    /// </summary>
    DirectoryInfo GetLogsDirectory();
}

internal sealed class ErrorLogger : IErrorLogger
{
    private readonly DirectoryInfo _logsDirectory;

    public ErrorLogger(CliExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        
        // Store logs in ~/.aspire/logs/
        var aspirePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire");
        var logsPath = Path.Combine(aspirePath, "logs");
        _logsDirectory = new DirectoryInfo(logsPath);
    }

    public DirectoryInfo GetLogsDirectory()
    {
        return _logsDirectory;
    }

    public string LogError(Exception exception, string? commandContext = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var logFilePath = GetLogFilePath();
        EnsureLogsDirectoryExists();

        var logContent = new StringBuilder();
        logContent.AppendLine("===========================================");
        logContent.AppendLine("Aspire CLI Error Log");
        logContent.AppendLine(CultureInfo.InvariantCulture, $"Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz}");
        logContent.AppendLine(CultureInfo.InvariantCulture, $"Local Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}");
        
        if (!string.IsNullOrEmpty(commandContext))
        {
            logContent.AppendLine(CultureInfo.InvariantCulture, $"Command: {commandContext}");
        }

        logContent.AppendLine(CultureInfo.InvariantCulture, $"Exception Type: {exception.GetType().FullName}");
        logContent.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
        
        if (exception.InnerException != null)
        {
            logContent.AppendLine();
            logContent.AppendLine("Inner Exception:");
            AppendExceptionDetails(logContent, exception.InnerException, indent: "  ");
        }

        logContent.AppendLine();
        logContent.AppendLine("Stack Trace:");
        logContent.AppendLine(exception.StackTrace ?? "(No stack trace available)");

        if (exception.Data.Count > 0)
        {
            logContent.AppendLine();
            logContent.AppendLine("Exception Data:");
            foreach (var key in exception.Data.Keys)
            {
                logContent.AppendLine(CultureInfo.InvariantCulture, $"  {key}: {exception.Data[key]}");
            }
        }

        logContent.AppendLine("===========================================");
        logContent.AppendLine();

        File.AppendAllText(logFilePath, logContent.ToString());
        return logFilePath;
    }

    public string LogError(string message, string? details = null, string? commandContext = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        var logFilePath = GetLogFilePath();
        EnsureLogsDirectoryExists();

        var logContent = new StringBuilder();
        logContent.AppendLine("===========================================");
        logContent.AppendLine("Aspire CLI Error Log");
        logContent.AppendLine(CultureInfo.InvariantCulture, $"Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz}");
        logContent.AppendLine(CultureInfo.InvariantCulture, $"Local Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}");
        
        if (!string.IsNullOrEmpty(commandContext))
        {
            logContent.AppendLine(CultureInfo.InvariantCulture, $"Command: {commandContext}");
        }

        logContent.AppendLine(CultureInfo.InvariantCulture, $"Message: {message}");

        if (!string.IsNullOrEmpty(details))
        {
            logContent.AppendLine();
            logContent.AppendLine("Details:");
            logContent.AppendLine(details);
        }

        logContent.AppendLine("===========================================");
        logContent.AppendLine();

        File.AppendAllText(logFilePath, logContent.ToString());
        return logFilePath;
    }

    private static void AppendExceptionDetails(StringBuilder logContent, Exception exception, string indent)
    {
        logContent.AppendLine(CultureInfo.InvariantCulture, $"{indent}Type: {exception.GetType().FullName}");
        logContent.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {exception.Message}");
        
        if (exception.StackTrace != null)
        {
            logContent.AppendLine(CultureInfo.InvariantCulture, $"{indent}Stack Trace:");
            foreach (var line in exception.StackTrace.Split('\n'))
            {
                logContent.AppendLine(CultureInfo.InvariantCulture, $"{indent}  {line.TrimEnd()}");
            }
        }

        if (exception.InnerException != null)
        {
            logContent.AppendLine();
            logContent.AppendLine(CultureInfo.InvariantCulture, $"{indent}Inner Exception:");
            AppendExceptionDetails(logContent, exception.InnerException, indent + "  ");
        }
    }

    private void EnsureLogsDirectoryExists()
    {
        if (!_logsDirectory.Exists)
        {
            _logsDirectory.Create();
        }
    }

    private string GetLogFilePath()
    {
        // Create a log file per day to avoid huge files
        // Use UtcNow for consistency with log timestamps
        // File.AppendAllText is used for writing, which is safe for concurrent access
        // as the OS handles file locking
        var fileName = $"aspire-cli-{DateTime.UtcNow:yyyy-MM-dd}.log";
        return Path.Combine(_logsDirectory.FullName, fileName);
    }
}
