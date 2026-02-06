// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOTNETTOOL

using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Orchestrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Executes EF Core design-time operations by running a DotnetToolResource.
/// </summary>
/// <remarks>
/// This class uses a pre-created DotnetToolResource to execute EF Core operations through Aspire's orchestration.
/// The tool is automatically downloaded and run. The target project must reference Microsoft.EntityFrameworkCore.Design.
/// </remarks>
internal sealed class EFCoreOperationExecutor : IDisposable
{
    private readonly ProjectResource _startupProjectResource;
    private readonly IProjectMetadata? _targetProjectMetadata;
    private readonly string? _contextTypeName;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly IServiceProvider _serviceProvider;
    private readonly DotnetToolResource _toolResource;

    private string? _startupProjectPath;
    private string? _targetProjectPath;
    private string? _framework;
    private string? _configuration;
    private bool _initialized;

    public EFCoreOperationExecutor(
        ProjectResource startupProjectResource,
        IProjectMetadata? targetProjectMetadata,
        string? contextTypeName,
        ILogger logger,
        CancellationToken cancellationToken,
        IServiceProvider serviceProvider,
        DotnetToolResource toolResource)
    {
        _startupProjectResource = startupProjectResource;
        _targetProjectMetadata = targetProjectMetadata;
        _contextTypeName = contextTypeName;
        _logger = logger;
        _cancellationToken = cancellationToken;
        _serviceProvider = serviceProvider;
        _toolResource = toolResource;

        // Get build settings from the entry assembly (AppHost)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly?.Location is { } appHostAssemblyPath && !string.IsNullOrEmpty(appHostAssemblyPath))
        {
            // Get configuration from assembly attribute
            var configAttribute = entryAssembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
            if (configAttribute?.Configuration is { } configuration)
            {
                _configuration = configuration;
            }

            // Get target framework from assembly attribute
            var frameworkAttribute = entryAssembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            if (frameworkAttribute?.FrameworkName is { } frameworkName)
            {
                // FrameworkName is in format ".NETCoreApp,Version=v8.0" - extract the version part
                const string versionPrefix = ".NETCoreApp,Version=v";
                var versionIndex = frameworkName.IndexOf(versionPrefix, StringComparison.OrdinalIgnoreCase);
                if (versionIndex >= 0)
                {
                    var version = frameworkName.Substring(versionIndex + versionPrefix.Length);
                    _framework = $"net{version}";
                }
            }

            // Parse configuration, framework from the assembly path (if not found from attributes)
            ParseBuildSettingsFromPath(appHostAssemblyPath);
        }
    }

    private EFOperationResult EnsurePathsInitialized()
    {
        if (_initialized)
        {
            return _startupProjectPath != null
                ? new EFOperationResult { Success = true }
                : new EFOperationResult { Success = false, ErrorMessage = "Could not determine project path." };
        }

        _initialized = true;

        _startupProjectPath = GetProjectPath(_startupProjectResource);
        if (string.IsNullOrEmpty(_startupProjectPath))
        {
            return new EFOperationResult { Success = false, ErrorMessage = "Could not determine startup project path." };
        }

        _targetProjectPath = _targetProjectMetadata?.ProjectPath ?? _startupProjectPath;

        if (string.IsNullOrEmpty(_targetProjectPath))
        {
            return new EFOperationResult { Success = false, ErrorMessage = "Could not determine target project path." };
        }

        _logger.LogDebug("Using startup project: {StartupProject}", _startupProjectPath);
        _logger.LogDebug("Using target project: {TargetProject}", _targetProjectPath);

        var workingDir = Path.GetDirectoryName(_startupProjectPath);
        if (!string.IsNullOrEmpty(workingDir))
        {
            var execAnnotation = _toolResource.Annotations.OfType<ExecutableAnnotation>().FirstOrDefault();
            if (execAnnotation != null && execAnnotation.WorkingDirectory != workingDir)
            {
                _toolResource.Annotations.Remove(execAnnotation);
                _toolResource.Annotations.Add(new ExecutableAnnotation
                {
                    Command = execAnnotation.Command,
                    WorkingDirectory = workingDir
                });
            }
        }

        return new EFOperationResult { Success = true };
    }

    private void ParseBuildSettingsFromPath(string assemblyPath)
    {
        var segments = assemblyPath.Split(Path.DirectorySeparatorChar, '/');
        var binIndex = Array.FindLastIndex(segments, s => s.Equals("bin", StringComparison.OrdinalIgnoreCase));

        if (binIndex < 0 || binIndex + 2 >= segments.Length)
        {
            return;
        }

        // Typical layout: bin/Debug/net10.0/ or bin/Debug/net10.0/win-x64/
        var configIndex = binIndex + 1;
        var frameworkIndex = binIndex + 2;

        if (_configuration == null && configIndex < segments.Length)
        {
            var configCandidate = segments[configIndex];
            if (configCandidate.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                configCandidate.Equals("Release", StringComparison.OrdinalIgnoreCase))
            {
                _configuration = configCandidate;
            }
        }

        if (_framework == null && frameworkIndex < segments.Length)
        {
            var frameworkCandidate = segments[frameworkIndex];
            if (frameworkCandidate.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            {
                _framework = frameworkCandidate;
            }
        }
    }

    private static string? GetProjectPath(ProjectResource projectResource)
    {
        if (projectResource.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            return metadata.ProjectPath;
        }
        return null;
    }

    private async Task<EFOperationResult> ExecuteEfCommandAsync(string command, string subCommand, Dictionary<string, string?>? additionalArgs = null)
    {
        var initResult = EnsurePathsInitialized();
        if (!initResult.Success)
        {
            return initResult;
        }

        // Build the EF command arguments (these go after the -- in dotnet tool exec)
        var efArgs = new List<string> { command, subCommand, "--no-build" };

        // Add project paths (quoted for paths with spaces)
        efArgs.Add("--project");
        efArgs.Add($"\"{_targetProjectPath!}\"");

        if (_startupProjectPath != _targetProjectPath)
        {
            efArgs.Add("--startup-project");
            efArgs.Add($"\"{_startupProjectPath!}\"");
        }

        // Add configuration if available
        if (!string.IsNullOrEmpty(_configuration))
        {
            efArgs.Add("--configuration");
            efArgs.Add(_configuration);
        }

        // Add framework if available
        if (!string.IsNullOrEmpty(_framework))
        {
            efArgs.Add("--framework");
            efArgs.Add(_framework);
        }

        // Add context if specified
        if (!string.IsNullOrEmpty(_contextTypeName))
        {
            efArgs.Add("--context");
            efArgs.Add(_contextTypeName);
        }

        // Add additional arguments
        if (additionalArgs != null)
        {
            foreach (var (key, value) in additionalArgs)
            {
                efArgs.Add(key);
                if (!string.IsNullOrEmpty(value))
                {
                    efArgs.Add(value);
                }
            }
        }

        _logger.LogDebug("Executing EF command: {Command} {SubCommand} with args: {Args}", 
            command, subCommand, string.Join(" ", efArgs));

        try
        {
            // Get required services
            var orchestrator = _serviceProvider.GetRequiredService<ApplicationOrchestrator>();
            var notificationService = _serviceProvider.GetRequiredService<ResourceNotificationService>();
            var loggerService = _serviceProvider.GetRequiredService<ResourceLoggerService>();

            // Clear existing command-line args and set new ones for this specific command
            var existingArgsAnnotations = _toolResource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().ToList();
            foreach (var ann in existingArgsAnnotations)
            {
                _toolResource.Annotations.Remove(ann);
            }
            
            var argsAnnotation = new CommandLineArgsCallbackAnnotation(args =>
            {
                foreach (var arg in efArgs)
                {
                    args.Add(arg);
                }
            });
            _toolResource.Annotations.Add(argsAnnotation);

            // Capture output before starting
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            using var logCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

            try
            {
                // Start a background task to capture logs
                var logTask = CaptureLogsAsync(loggerService.WatchAsync(_toolResource), outputBuilder, errorBuilder, logCancellation.Token);

                // Start the resource
                await orchestrator.StartResourceAsync(_toolResource.Name, _cancellationToken).ConfigureAwait(false);

                // Wait for the resource to finish
                await notificationService.WaitForResourceAsync(
                    _toolResource.Name,
                    r => r.Snapshot.State?.Text == KnownResourceStates.Finished || 
                         r.Snapshot.State?.Text == KnownResourceStates.Exited ||
                         r.Snapshot.State?.Text == KnownResourceStates.FailedToStart,
                    _cancellationToken).ConfigureAwait(false);

                // Get final state
                var resourceEvent = await notificationService.WaitForResourceAsync(
                    _toolResource.Name,
                    _ => true, // Just get current state
                    _cancellationToken).ConfigureAwait(false);

                var snapshot = resourceEvent.Snapshot;

                // Give a moment for logs to flush, then cancel log capture
                await Task.Delay(200, _cancellationToken).ConfigureAwait(false);
                await logCancellation.CancelAsync().ConfigureAwait(false);
                
                try
                {
                    await logTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when we cancel the log capture
                }

                var stdout = outputBuilder.ToString();
                var stderr = errorBuilder.ToString();

                // Log output
                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    _logger.LogDebug("dotnet-ef stdout: {Output}", stdout);
                }
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    _logger.LogDebug("dotnet-ef stderr: {Output}", stderr);
                }

                // Check if the command succeeded
                var exitCode = snapshot.Properties.FirstOrDefault(p => p.Name == "ExitCode")?.Value?.ToString();
                if (exitCode != "0" && snapshot.State?.Text == KnownResourceStates.FailedToStart)
                {
                    var errorMessage = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
                    return new EFOperationResult
                    {
                        Success = false,
                        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "dotnet-ef command failed" : errorMessage
                    };
                }

                return new EFOperationResult { Success = true, Output = stdout };
            }
            finally
            {
                // Clean up command-specific annotations
                _toolResource.Annotations.Remove(argsAnnotation);
            }
        }
        catch (Exception ex)
        {
            return new EFOperationResult { Success = false, ErrorMessage = $"dotnet-ef command failed: {ex.Message}" };
        }
    }

    private static async Task CaptureLogsAsync(
        IAsyncEnumerable<IReadOnlyList<LogLine>> logChannel,
        StringBuilder outputBuilder,
        StringBuilder errorBuilder,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entries in logChannel.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (var entry in entries)
                {
                    if (entry.IsErrorMessage)
                    {
                        errorBuilder.AppendLine(entry.Content);
                    }
                    else
                    {
                        outputBuilder.AppendLine(entry.Content);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    public async Task<EFOperationResult> UpdateDatabaseAsync()
    {
        _logger.LogInformation("Updating database...");
        return await ExecuteEfCommandAsync("database", "update").ConfigureAwait(false);
    }

    public async Task<EFOperationResult> DropDatabaseAsync()
    {
        _logger.LogInformation("Dropping database...");
        return await ExecuteEfCommandAsync("database", "drop", new Dictionary<string, string?>
        {
            { "--force", null }
        }).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> ResetDatabaseAsync()
    {
        // First drop the database
        var dropResult = await DropDatabaseAsync().ConfigureAwait(false);
        if (!dropResult.Success)
        {
            // If drop fails because database doesn't exist, continue with update
            if (!dropResult.ErrorMessage?.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return dropResult;
            }
        }

        // Then update (recreate with migrations)
        return await UpdateDatabaseAsync().ConfigureAwait(false);
    }

    public async Task<EFOperationResult> AddMigrationAsync(string? migrationName = null, string? outputDir = null, string? @namespace = null)
    {
        migrationName ??= $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";
        _logger.LogInformation("Creating migration with name: {MigrationName}", migrationName);

        var args = new Dictionary<string, string?>
        {
            { migrationName, null } // Migration name is positional
        };

        if (!string.IsNullOrEmpty(outputDir))
        {
            args["--output-dir"] = $"\"{outputDir}\"";
        }

        if (!string.IsNullOrEmpty(@namespace))
        {
            args["--namespace"] = @namespace;
        }

        return await ExecuteEfCommandAsync("migrations", "add", args).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> RemoveMigrationAsync()
    {
        _logger.LogInformation("Removing last migration...");
        return await ExecuteEfCommandAsync("migrations", "remove", new Dictionary<string, string?>
        {
            { "--force", null }
        }).ConfigureAwait(false);
    }

    public async Task<EFOperationResult> GetDatabaseStatusAsync()
    {
        _logger.LogInformation("Getting migration status...");

        // Use migrations list --json to get structured output
        var result = await ExecuteEfCommandAsync("migrations", "list", new Dictionary<string, string?>
        {
            { "--json", null }
        }).ConfigureAwait(false);

        if (!result.Success)
        {
            // Handle case where database doesn't exist
            if (result.ErrorMessage?.Contains("does not exist", StringComparison.OrdinalIgnoreCase) == true ||
                result.ErrorMessage?.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new EFOperationResult
                {
                    Success = true,
                    Output = "**Database Status:** Database has not been created yet.\n\nRun 'Update Database' to create and apply migrations."
                };
            }
            return result;
        }

        // Parse JSON output from dotnet ef migrations list --json
        var migrationLines = new List<string>();
        var pendingCount = 0;
        var appliedCount = 0;

        try
        {
            // The JSON output is an array of migration objects with "id", "name", and "applied" properties
            using var doc = JsonDocument.Parse(result.Output ?? "[]");
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var migration in doc.RootElement.EnumerateArray())
                {
                    var id = migration.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    var name = migration.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    var applied = migration.TryGetProperty("applied", out var appliedProp) && appliedProp.GetBoolean();

                    var displayName = id ?? name ?? "Unknown";

                    if (applied)
                    {
                        migrationLines.Add($"- ✅ {displayName}");
                        appliedCount++;
                    }
                    else
                    {
                        migrationLines.Add($"- ⚠️ {displayName} (Pending)");
                        pendingCount++;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Fallback to parsing text output if JSON parsing fails
            return ParseTextMigrationOutput(result.Output);
        }

        var summary = new StringBuilder();
        summary.AppendLine(CultureInfo.InvariantCulture, $"**Applied Migrations:** {appliedCount}");
        summary.AppendLine(CultureInfo.InvariantCulture, $"**Pending Migrations:** {pendingCount}");
        summary.AppendLine();

        if (migrationLines.Count > 0)
        {
            summary.AppendLine("**Migration History:**");
            summary.AppendLine();
            foreach (var line in migrationLines)
            {
                summary.AppendLine(line);
            }
        }
        else
        {
            summary.AppendLine("No migrations found.");
        }

        // Check for pending model changes
        var pendingChangesResult = await ExecuteEfCommandAsync("migrations", "has-pending-model-changes").ConfigureAwait(false);
        summary.AppendLine();
        summary.AppendLine("**Model Changes:**");
        if (!pendingChangesResult.Success)
        {
            // Command may not be available in older EF Core versions
            summary.AppendLine("ℹ️ Unable to check for pending model changes.");
        }
        else if (pendingChangesResult.Output?.Contains("Changes have been made", StringComparison.OrdinalIgnoreCase) == true ||
                 pendingChangesResult.Output?.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) == true)
        {
            summary.AppendLine("⚠️ Pending model changes detected. A new migration may be needed.");
        }
        else
        {
            summary.AppendLine("✅ No pending model changes.");
        }

        return new EFOperationResult { Success = true, Output = summary.ToString() };
    }

    private static EFOperationResult ParseTextMigrationOutput(string? output)
    {
        var lines = output?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var migrationLines = new List<string>();
        var pendingCount = 0;
        var appliedCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            // dotnet ef migrations list output format:
            // - Applied migrations show without "(Pending)" suffix
            // - Pending migrations show with "(Pending)" suffix
            if (trimmed.Contains("(Pending)", StringComparison.OrdinalIgnoreCase))
            {
                var name = trimmed.Replace("(Pending)", "", StringComparison.OrdinalIgnoreCase).Trim();
                migrationLines.Add($"- ⚠️ {name} (Pending)");
                pendingCount++;
            }
            else if (!trimmed.StartsWith("Build", StringComparison.OrdinalIgnoreCase) &&
                     !trimmed.StartsWith("Done", StringComparison.OrdinalIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(trimmed))
            {
                migrationLines.Add($"- ✅ {trimmed}");
                appliedCount++;
            }
        }

        var summary = new StringBuilder();
        summary.AppendLine(CultureInfo.InvariantCulture, $"**Applied Migrations:** {appliedCount}");
        summary.AppendLine(CultureInfo.InvariantCulture, $"**Pending Migrations:** {pendingCount}");
        summary.AppendLine();

        if (migrationLines.Count > 0)
        {
            summary.AppendLine("**Migration History:**");
            summary.AppendLine();
            foreach (var line in migrationLines)
            {
                summary.AppendLine(line);
            }
        }
        else
        {
            summary.AppendLine("No migrations found.");
        }

        return new EFOperationResult { Success = true, Output = summary.ToString() };
    }

    /// <summary>
    /// Generates a SQL migration script.
    /// </summary>
    /// <param name="outputPath">The output path for the script file.</param>
    /// <param name="idempotent">If true, generates an idempotent script with IF NOT EXISTS checks.</param>
    /// <param name="noTransactions">If true, omits transaction statements from the script.</param>
    /// <returns>The operation result.</returns>
    public async Task<EFOperationResult> GenerateMigrationScriptAsync(string? outputPath = null, bool idempotent = true, bool noTransactions = false)
    {
        _logger.LogInformation("Generating migration script...");

        var args = new Dictionary<string, string?>();

        if (idempotent)
        {
            args["--idempotent"] = null;
        }

        if (noTransactions)
        {
            args["--no-transactions"] = null;
        }

        if (!string.IsNullOrEmpty(outputPath))
        {
            args["--output"] = $"\"{outputPath}\"";
        }

        return await ExecuteEfCommandAsync("migrations", "script", args).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a migration bundle executable.
    /// </summary>
    /// <param name="outputPath">The output path for the bundle file.</param>
    /// <param name="targetRuntime">The target runtime identifier (e.g., "linux-x64", "win-x64").</param>
    /// <param name="selfContained">If true, creates a self-contained bundle that includes the .NET runtime.</param>
    /// <returns>The operation result.</returns>
    public async Task<EFOperationResult> GenerateMigrationBundleAsync(string? outputPath = null, string? targetRuntime = null, bool selfContained = false)
    {
        _logger.LogInformation("Generating migration bundle...");

        var args = new Dictionary<string, string?>();

        if (!string.IsNullOrEmpty(outputPath))
        {
            args["--output"] = $"\"{outputPath}\"";
        }

        if (!string.IsNullOrEmpty(targetRuntime))
        {
            args["--runtime"] = targetRuntime;
        }

        if (selfContained)
        {
            args["--self-contained"] = null;
        }

        return await ExecuteEfCommandAsync("migrations", "bundle", args).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // Nothing to dispose - processes are cleaned up after each operation
    }
}

/// <summary>
/// Result of an EF Core operation.
/// </summary>
internal sealed class EFOperationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Output { get; init; }
}
