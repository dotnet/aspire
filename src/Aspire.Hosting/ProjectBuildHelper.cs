// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides utilities for querying MSBuild project information.
/// </summary>
internal static class ProjectBuildHelper
{
    private const int MaxRetries = 3;

    /// <summary>
    /// Queries MSBuild for the set of source files and dependencies that form a project's compilation closure.
    /// </summary>
    /// <param name="projectPath">The path to the .csproj file.</param>
    /// <param name="logger">A logger for diagnostics.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ProjectFileClosure"/> containing the file set and timestamps, or null if the query fails.</returns>
    public static async Task<ProjectFileClosure?> GetProjectFileClosureAsync(string projectPath, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var json = await EvaluateMSBuildAsync(projectPath, logger, cancellationToken).ConfigureAwait(false);

            var projectDir = Path.GetDirectoryName(projectPath)!;
            var fileTimestamps = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            // Always track the project file itself.
            RecordFileTimestamp(fileTimestamps, projectPath);

            if (json is not null)
            {
                ParseEvaluationOutput(json, projectDir, fileTimestamps, logger);
                logger.LogDebug("Captured file closure for {ProjectPath}: {FileCount} files", projectPath, fileTimestamps.Count);
            }
            else
            {
                // Fallback: just track the project file and nearby .cs files.
                logger.LogDebug("Falling back to directory scan for {ProjectPath}", projectPath);
                ScanDirectoryForSourceFiles(projectDir, fileTimestamps);
            }

            return new ProjectFileClosure
            {
                FileTimestamps = fileTimestamps,
                ProjectPath = projectPath,
                CapturedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Error getting project file closure for {ProjectPath}", projectPath);
            return null;
        }
    }

    /// <summary>
    /// Runs <c>dotnet msbuild -getItem:Compile,ProjectReference -getProperty:MSBuildVersion</c> against the project.
    /// Includes retry logic for MSBuild server contention (empty output on success).
    /// </summary>
    private static async Task<JsonDocument?> EvaluateMSBuildAsync(string projectPath, ILogger logger, CancellationToken cancellationToken)
    {
        // Use -getProperty:MSBuildVersion as a workaround for MSBuild bug #12490 — a single property
        // query doesn't produce valid JSON output. MSBuildVersion is harmless and always present.
        var arguments = $"msbuild -getItem:Compile,ProjectReference -getProperty:MSBuildVersion \"{projectPath}\"";

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var outputLines = new List<string>();
            var errorLines = new List<string>();

            var spec = new ProcessSpec("dotnet")
            {
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(projectPath),
                OnOutputData = output =>
                {
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        outputLines.Add(output);
                    }
                },
                OnErrorData = error =>
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        errorLines.Add(error);
                    }
                },
                ThrowOnNonZeroReturnCode = false
            };

            logger.LogDebug("Evaluating MSBuild for {ProjectPath} (attempt {Attempt}/{MaxRetries})", projectPath, attempt + 1, MaxRetries);
            var (pendingResult, processDisposable) = ProcessUtil.Run(spec);

            ProcessResult result;
            await using (processDisposable.ConfigureAwait(false))
            {
                result = await pendingResult.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (result.ExitCode != 0)
            {
                logger.LogDebug("dotnet msbuild evaluation failed for {ProjectPath}. Exit code: {ExitCode}. Stderr: {Stderr}",
                    projectPath, result.ExitCode, string.Join('\n', errorLines));
                return null;
            }

            var stdout = string.Join('\n', outputLines);

            if (string.IsNullOrWhiteSpace(stdout))
            {
                // MSBuild server contention can produce exit code 0 but no output. Retry.
                if (attempt < MaxRetries - 1)
                {
                    logger.LogDebug(
                        "dotnet msbuild returned exit code 0 but produced no output (attempt {Attempt}/{MaxRetries}). Retrying after delay.",
                        attempt + 1, MaxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(attempt + 1), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                logger.LogDebug("dotnet msbuild returned exit code 0 but produced no output after {MaxRetries} attempts.", MaxRetries);
                return null;
            }

            try
            {
                return JsonDocument.Parse(stdout);
            }
            catch (JsonException ex)
            {
                logger.LogDebug(ex, "Failed to parse MSBuild JSON output for {ProjectPath}", projectPath);
                return null;
            }
        }

        return null;
    }

    private static void ParseEvaluationOutput(JsonDocument doc, string projectDir, Dictionary<string, DateTime> fileTimestamps, ILogger logger)
    {
        try
        {
            if (doc.RootElement.TryGetProperty("Items", out var items))
            {
                // Compile items use Identity (relative path).
                if (items.TryGetProperty("Compile", out var compileItems) && compileItems.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in compileItems.EnumerateArray())
                    {
                        var path = item.TryGetProperty("FullPath", out var fullPath)
                            ? fullPath.GetString()
                            : item.TryGetProperty("Identity", out var identity)
                                ? identity.GetString()
                                : null;

                        if (!string.IsNullOrEmpty(path))
                        {
                            var absolutePath = Path.IsPathRooted(path) ? path : Path.GetFullPath(path, projectDir);
                            RecordFileTimestamp(fileTimestamps, absolutePath);
                        }
                    }
                }

                // ProjectReference items use FullPath (absolute) when available.
                if (items.TryGetProperty("ProjectReference", out var projRefItems) && projRefItems.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in projRefItems.EnumerateArray())
                    {
                        var path = item.TryGetProperty("FullPath", out var fullPath)
                            ? fullPath.GetString()
                            : item.TryGetProperty("Identity", out var identity)
                                ? identity.GetString()
                                : null;

                        if (!string.IsNullOrEmpty(path))
                        {
                            var absolutePath = Path.IsPathRooted(path) ? path : Path.GetFullPath(path, projectDir);
                            RecordFileTimestamp(fileTimestamps, absolutePath);
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Error parsing MSBuild evaluation output");
        }
    }

    private static void RecordFileTimestamp(Dictionary<string, DateTime> timestamps, string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                timestamps[filePath] = File.GetLastWriteTimeUtc(filePath);
            }
        }
        catch (IOException)
        {
            // File may be locked or inaccessible; skip it.
        }
    }

    private static void ScanDirectoryForSourceFiles(string directory, Dictionary<string, DateTime> fileTimestamps)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                // Skip obj and bin directories.
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                RecordFileTimestamp(fileTimestamps, file);
            }

            // Also track .csproj files in the directory.
            foreach (var file in Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly))
            {
                RecordFileTimestamp(fileTimestamps, file);
            }
        }
        catch (IOException)
        {
            // Directory may not exist or be inaccessible.
        }
    }
}
