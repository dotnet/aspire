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
/// Implementation of diagnostics bundle writer that creates a timestamped folder
/// with diagnostic information when commands fail.
/// </summary>
internal sealed class DiagnosticsBundleWriter : IDiagnosticsBundleWriter
{
    private readonly CliExecutionContext _executionContext;
    private readonly ILogger<DiagnosticsBundleWriter> _logger;
    private readonly TimeProvider _timeProvider;

    public DiagnosticsBundleWriter(
        CliExecutionContext executionContext,
        ILogger<DiagnosticsBundleWriter> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _executionContext = executionContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<string?> WriteFailureBundleAsync(
        Exception exception,
        int exitCode,
        string commandName,
        string? additionalContext = null)
    {
        try
        {
            var timestamp = _timeProvider.GetUtcNow().ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            var bundleDirectory = GetDiagnosticsBundleDirectory(timestamp);
            
            if (!bundleDirectory.Exists)
            {
                bundleDirectory.Create();
            }

            // Write error.txt
            await WriteErrorFileAsync(bundleDirectory, exception, exitCode, commandName, additionalContext);

            // Write environment.json
            await WriteEnvironmentFileAsync(bundleDirectory);

            // Write aspire.log (placeholder for now - full session logging will be added separately)
            await WriteLogFileAsync(bundleDirectory, exception, commandName);

            return bundleDirectory.FullName;
        }
        catch (Exception ex)
        {
            // Don't let diagnostics writing itself fail the command
            _logger.LogError(ex, "Failed to write diagnostics bundle");
            return null;
        }
    }

    private DirectoryInfo GetDiagnosticsBundleDirectory(string timestamp)
    {
        var diagnosticsPath = Path.Combine(
            _executionContext.HomeDirectory.FullName,
            ".aspire",
            "cli",
            "diagnostics",
            timestamp);
        return new DirectoryInfo(diagnosticsPath);
    }

    private static async Task WriteErrorFileAsync(
        DirectoryInfo bundleDirectory,
        Exception exception,
        int exitCode,
        string commandName,
        string? additionalContext)
    {
        var errorFilePath = Path.Combine(bundleDirectory.FullName, "error.txt");
        var sb = new StringBuilder();

        sb.AppendLine("Aspire CLI Failure Report");
        sb.AppendLine("=========================");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Command: aspire {commandName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Exit Code: {exitCode}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(additionalContext))
        {
            sb.AppendLine("Additional Context:");
            sb.AppendLine(additionalContext);
            sb.AppendLine();
        }

        sb.AppendLine("Error Summary:");
        sb.AppendLine("--------------");
        sb.AppendLine(exception.Message);
        sb.AppendLine();

        sb.AppendLine("Exception Details:");
        sb.AppendLine("------------------");
        sb.AppendLine(FormatException(exception));

        await File.WriteAllTextAsync(errorFilePath, sb.ToString());
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
                break; // Don't continue with innerException for AggregateException
            }

            currentException = currentException.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    private async Task WriteEnvironmentFileAsync(DirectoryInfo bundleDirectory)
    {
        var environmentFilePath = Path.Combine(bundleDirectory.FullName, "environment.json");
        
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  \"cli\": {{");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"version\": \"{JsonEncodedText.Encode(PackageUpdateHelpers.GetCurrentAssemblyVersion() ?? "unknown")}\",");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"debugMode\": {(_executionContext.DebugMode ? "true" : "false")},");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"verboseMode\": {(_executionContext.VerboseMode ? "true" : "false")}");
        sb.AppendLine("  },");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  \"os\": {{");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"platform\": \"{JsonEncodedText.Encode(RuntimeInformation.OSDescription)}\",");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"architecture\": \"{RuntimeInformation.OSArchitecture}\",");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"version\": \"{JsonEncodedText.Encode(Environment.OSVersion.VersionString)}\",");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"is64Bit\": {(Environment.Is64BitOperatingSystem ? "true" : "false")}");
        sb.AppendLine("  },");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  \"dotnet\": {{");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"runtimeVersion\": \"{JsonEncodedText.Encode(RuntimeInformation.FrameworkDescription)}\",");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"processArchitecture\": \"{RuntimeInformation.ProcessArchitecture}\"");
        sb.AppendLine("  },");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  \"process\": {{");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"processId\": {Environment.ProcessId},");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"workingDirectory\": \"{JsonEncodedText.Encode(_executionContext.WorkingDirectory.FullName)}\",");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"userName\": \"{JsonEncodedText.Encode(Environment.UserName)}\",");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    \"machineName\": \"{JsonEncodedText.Encode(Environment.MachineName)}\"");
        sb.AppendLine("  },");
        
        var dockerInfo = await GetDockerInfoStringAsync();
        sb.AppendLine(CultureInfo.InvariantCulture, $"  \"docker\": {dockerInfo},");
        
        var envVars = GetRelevantEnvironmentVariables();
        sb.AppendLine("  \"environment\": {");
        var first = true;
        foreach (var kvp in envVars)
        {
            if (!first)
            {
                sb.AppendLine(",");
            }
            sb.Append(CultureInfo.InvariantCulture, $"    \"{JsonEncodedText.Encode(kvp.Key)}\": \"{JsonEncodedText.Encode(kvp.Value ?? "")}\"");
            first = false;
        }
        sb.AppendLine();
        sb.AppendLine("  }");
        sb.AppendLine("}");

        await File.WriteAllTextAsync(environmentFilePath, sb.ToString());
    }

    private static async Task<string> GetDockerInfoStringAsync()
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
                return """{ "available": false, "error": "Failed to start docker process" }""";
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
                    return $"{{ \"available\": true, \"clientVersion\": \"{JsonEncodedText.Encode(clientVersion ?? "unknown")}\", \"serverVersion\": \"{JsonEncodedText.Encode(serverVersion ?? "unknown")}\" }}";
                }
                catch
                {
                    return """{ "available": true, "version": "unknown" }""";
                }
            }

            return """{ "available": false, "error": "Docker command failed" }""";
        }
        catch (Exception ex)
        {
            var encodedMessage = JsonEncodedText.Encode(ex.Message);
            return $"{{ \"available\": false, \"error\": \"{encodedMessage}\" }}";
        }
    }

    private static Dictionary<string, string?> GetRelevantEnvironmentVariables()
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

        var envVars = new Dictionary<string, string?>();
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

    private static async Task WriteLogFileAsync(
        DirectoryInfo bundleDirectory,
        Exception exception,
        string commandName)
    {
        var logFilePath = Path.Combine(bundleDirectory.FullName, "aspire.log");
        var sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture, $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}] Command started: aspire {commandName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}] Command failed with exception: {exception.GetType().Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}] Message: {exception.Message}");
        sb.AppendLine();
        sb.AppendLine("Note: Full session logging will be available in future versions.");

        await File.WriteAllTextAsync(logFilePath, sb.ToString());
    }
}
