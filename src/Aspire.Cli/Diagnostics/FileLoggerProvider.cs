// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Diagnostics;

/// <summary>
/// Writes all logs continuously to aspire.log, and automatically creates error.txt and environment.json when Error/Critical logs are encountered.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly CliExecutionContext _executionContext;
    private readonly TimeProvider _timeProvider;
    private readonly string _timestamp;
    private readonly DirectoryInfo? _diagnosticsDirectory;
    private readonly StreamWriter? _logWriter;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _environmentWritten;
    private bool _disposed;

    public FileLoggerProvider(CliExecutionContext executionContext, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _executionContext = executionContext;
        _timeProvider = timeProvider;
        _timestamp = _timeProvider.GetUtcNow().ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
        
        try
        {
            _diagnosticsDirectory = GetDiagnosticsBundleDirectory();
            if (!_diagnosticsDirectory.Exists)
            {
                _diagnosticsDirectory.Create();
            }

            var logFilePath = Path.Combine(_diagnosticsDirectory.FullName, "aspire.log");
            _logWriter = new StreamWriter(logFilePath, append: true)
            {
                AutoFlush = true
            };
        }
        catch
        {
            // If we can't create the directory or file, just continue without file logging
            _logWriter = null;
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(this, categoryName);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Wait for any pending writes to complete before disposing
        _writeLock.Wait();
        try
        {
            _logWriter?.Dispose();
        }
        finally
        {
            _writeLock.Release();
            _writeLock.Dispose();
        }
    }

    private DirectoryInfo GetDiagnosticsBundleDirectory()
    {
        var diagnosticsPath = Path.Combine(
            _executionContext.HomeDirectory.FullName,
            ".aspire",
            "cli",
            "diagnostics",
            _timestamp);
        return new DirectoryInfo(diagnosticsPath);
    }

    internal async Task WriteLogEntryAsync(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        if (_logWriter == null || _disposed)
        {
            return;
        }

        await _writeLock.WaitAsync();
        try
        {
            var timestamp = _timeProvider.GetUtcNow();
            var logEntry = $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{GetLogLevelString(logLevel)}] {categoryName}: {message}";
            
            await _logWriter.WriteLineAsync(logEntry);

            if (exception != null)
            {
                await _logWriter.WriteLineAsync($"Exception: {exception}");
            }

            // On first error or critical log, write environment snapshot
            if (!_environmentWritten && (logLevel == LogLevel.Error || logLevel == LogLevel.Critical))
            {
                _environmentWritten = true;
                await WriteEnvironmentSnapshotAsync();
            }

            // If this is an error/critical with exception, also write error.txt
            if (exception != null && (logLevel == LogLevel.Error || logLevel == LogLevel.Critical))
            {
                await WriteErrorFileAsync(exception, categoryName, message);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Waits for any pending async write operations to complete. For use in tests.
    /// </summary>
    internal async Task FlushAsync()
    {
        // Acquire and release the write lock to ensure all pending writes have completed
        await _writeLock.WaitAsync();
        try
        {
            // All writes are now complete
        }
        finally
        {
            _writeLock.Release();
        }
    }

    internal string? GetDiagnosticsPath()
    {
        return _diagnosticsDirectory?.Exists == true ? _diagnosticsDirectory.FullName : null;
    }

    private async Task WriteEnvironmentSnapshotAsync()
    {
        if (_diagnosticsDirectory == null)
        {
            return;
        }

        try
        {
            var environmentFilePath = Path.Combine(_diagnosticsDirectory.FullName, "environment.json");
            
            var dockerInfo = await GetDockerInfoAsync();
            var envVars = GetRelevantEnvironmentVariables();

            var snapshot = new EnvironmentSnapshot
            {
                Cli = new CliInfo
                {
                    Version = PackageUpdateHelpers.GetCurrentAssemblyVersion() ?? "unknown",
                    DebugMode = _executionContext.DebugMode,
                    VerboseMode = _executionContext.VerboseMode
                },
                Os = new OsInfo
                {
                    Platform = RuntimeInformation.OSDescription,
                    Architecture = RuntimeInformation.OSArchitecture.ToString(),
                    Version = Environment.OSVersion.VersionString,
                    Is64Bit = Environment.Is64BitOperatingSystem
                },
                DotNet = new DotNetInfo
                {
                    RuntimeVersion = RuntimeInformation.FrameworkDescription,
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
                },
                Process = new ProcessInfo
                {
                    ProcessId = Environment.ProcessId,
                    WorkingDirectory = _executionContext.WorkingDirectory.FullName,
                    UserName = Environment.UserName,
                    MachineName = Environment.MachineName
                },
                Docker = dockerInfo,
                Environment = envVars
            };

            var json = JsonSerializer.Serialize(snapshot, JsonSourceGenerationContext.Default.EnvironmentSnapshot);
            await File.WriteAllTextAsync(environmentFilePath, json);
        }
        catch
        {
            // Ignore errors writing environment snapshot
        }
    }

    private async Task WriteErrorFileAsync(Exception exception, string categoryName, string message)
    {
        if (_diagnosticsDirectory == null)
        {
            return;
        }

        try
        {
            var errorFilePath = Path.Combine(_diagnosticsDirectory.FullName, "error.txt");
            var sb = new StringBuilder();

            sb.AppendLine("Aspire CLI Failure Report");
            sb.AppendLine("=========================");
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Command: aspire {_executionContext.Command?.Name ?? "unknown"}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Category: {categoryName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Timestamp: {_timeProvider.GetUtcNow():yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            sb.AppendLine("Error Summary:");
            sb.AppendLine("--------------");
            sb.AppendLine(message);
            sb.AppendLine();

            sb.AppendLine("Exception Details:");
            sb.AppendLine("------------------");
            sb.AppendLine(FormatException(exception));

            await File.WriteAllTextAsync(errorFilePath, sb.ToString());
        }
        catch
        {
            // Ignore errors writing error file
        }
    }

    private static string FormatException(Exception exception)
    {
        var sb = new StringBuilder();
        var currentException = exception;
        var depth = 0;

        while (currentException != null)
        {
            if (depth > 0)
            {
                sb.AppendLine();
                sb.AppendLine(CultureInfo.InvariantCulture, $"Inner Exception ({depth}):");
                sb.AppendLine("-------------------");
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"Type: {currentException.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {currentException.Message}");
            
            if (!string.IsNullOrEmpty(currentException.StackTrace))
            {
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(currentException.StackTrace);
            }

            if (currentException is AggregateException aggregateException)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Inner Exceptions Count: {aggregateException.InnerExceptions.Count}");
                for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                {
                    sb.AppendLine();
                    sb.AppendLine(CultureInfo.InvariantCulture, $"Aggregate Inner Exception [{i}]:");
                    sb.AppendLine(FormatException(aggregateException.InnerExceptions[i]));
                }
                break;
            }

            currentException = currentException.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    private static async Task<DockerInfo> GetDockerInfoAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "version --format json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new DockerInfo
                {
                    Available = false,
                    Error = "Failed to start docker process"
                };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    using var doc = JsonDocument.Parse(output);
                    var clientVersion = doc.RootElement.TryGetProperty("Client", out var client) && 
                                       client.TryGetProperty("Version", out var cv) ? cv.GetString() : "unknown";
                    var serverVersion = doc.RootElement.TryGetProperty("Server", out var server) && 
                                       server.TryGetProperty("Version", out var sv) ? sv.GetString() : "unknown";
                    return new DockerInfo
                    {
                        Available = true,
                        ClientVersion = clientVersion ?? "unknown",
                        ServerVersion = serverVersion ?? "unknown"
                    };
                }
                catch
                {
                    return new DockerInfo
                    {
                        Available = true,
                        ClientVersion = "unknown"
                    };
                }
            }

            return new DockerInfo
            {
                Available = false,
                Error = "Docker command failed"
            };
        }
        catch (Exception ex)
        {
            return new DockerInfo
            {
                Available = false,
                Error = ex.Message
            };
        }
    }

    private static Dictionary<string, string> GetRelevantEnvironmentVariables()
    {
        var relevantVars = new[]
        {
            "ASPIRE_PLAYGROUND",
            "ASPIRE_NON_INTERACTIVE",
            "ASPIRE_ANSI_PASS_THRU",
            "ASPIRE_CONSOLE_WIDTH",
            "CI",
            "GITHUB_ACTIONS",
            "AZURE_PIPELINES",
            "PATH",
            "DOTNET_ROOT",
            "DOTNET_CLI_HOME",
            "NUGET_PACKAGES"
        };

        var envVars = new Dictionary<string, string>();
        foreach (var varName in relevantVars)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            if (value != null)
            {
                envVars[varName] = value;
            }
        }

        return envVars;
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => logLevel.ToString().ToLowerInvariant()
    };

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _categoryName;

        public FileLogger(FileLoggerProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception is null)
            {
                return;
            }

            // Fire and forget - don't block on file I/O
            _ = _provider.WriteLogEntryAsync(logLevel, _categoryName, message, exception);
        }
    }
}
