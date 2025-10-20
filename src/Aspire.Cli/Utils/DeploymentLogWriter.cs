// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Cli.Utils;

/// <summary>
/// Writes deployment logs to disk for later review.
/// </summary>
internal sealed class DeploymentLogWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly string _logFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentLogWriter"/> class.
    /// </summary>
    /// <param name="appHostSha">The SHA256 hash of the AppHost path, used to organize logs by AppHost.</param>
    public DeploymentLogWriter(string appHostSha)
    {
        _logFilePath = GetDeploymentLogPath(appHostSha);
        
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
            _writer = new StreamWriter(_logFilePath, append: false)
            {
                AutoFlush = true
            };
        }
        catch (Exception ex)
        {
            // If we can't create the log file, we'll just skip logging
            // This shouldn't fail the deployment
            throw new InvalidOperationException($"Failed to create deployment log file at {_logFilePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the path to the deployment log file.
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// Writes a line to the deployment log with a timestamp.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void WriteLine(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            _writer.WriteLine($"[{timestamp}] {message}");
        }
        catch
        {
            // Best-effort logging - don't fail deployment if logging fails
        }
    }

    /// <summary>
    /// Disposes the log writer and ensures all data is written to disk.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _writer?.Dispose();
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    /// <summary>
    /// Gets the deployment log path for the given AppHost SHA.
    /// </summary>
    /// <param name="appHostSha">The SHA256 hash of the AppHost path.</param>
    /// <returns>The full path to the deployment log file.</returns>
    private static string GetDeploymentLogPath(string appHostSha)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var aspireDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "deployments",
            appHostSha
        );

        return Path.Combine(aspireDir, $"{timestamp}.log");
    }
}
