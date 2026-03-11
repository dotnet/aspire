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
            var outputLines = new List<string>();
            var spec = new ProcessSpec("dotnet")
            {
                Arguments = $"msbuild -getItem:Compile,ProjectReference \"{projectPath}\"",
                WorkingDirectory = Path.GetDirectoryName(projectPath),
                OnOutputData = output =>
                {
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        outputLines.Add(output.Trim());
                    }
                },
                OnErrorData = error => logger.LogDebug("dotnet msbuild (stderr): {Error}", error),
                ThrowOnNonZeroReturnCode = false
            };

            logger.LogDebug("Getting project file closure for {ProjectPath}", projectPath);
            var (pendingResult, processDisposable) = ProcessUtil.Run(spec);

            await using (processDisposable.ConfigureAwait(false))
            {
                var result = await pendingResult.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (result.ExitCode != 0)
                {
                    logger.LogDebug("Failed to get file closure from dotnet msbuild for project {ProjectPath}. Exit code: {ExitCode}",
                        projectPath, result.ExitCode);
                    return null;
                }
            }

            var allOutput = string.Join('\n', outputLines);
            var projectDir = Path.GetDirectoryName(projectPath)!;
            var fileTimestamps = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            // Add the project file itself.
            RecordFileTimestamp(fileTimestamps, projectPath);

            // Parse the JSON output from MSBuild -getItem.
            if (TryParseItems(allOutput, projectDir, fileTimestamps, logger))
            {
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

    private static bool TryParseItems(string jsonOutput, string projectDir, Dictionary<string, DateTime> fileTimestamps, ILogger logger)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonOutput);

            if (doc.RootElement.TryGetProperty("Items", out var items))
            {
                ParseItemGroup(items, "Compile", projectDir, fileTimestamps);
                ParseItemGroup(items, "ProjectReference", projectDir, fileTimestamps);
            }

            return true;
        }
        catch (JsonException ex)
        {
            logger.LogDebug(ex, "Failed to parse MSBuild JSON output");
            return false;
        }
    }

    private static void ParseItemGroup(JsonElement items, string itemType, string projectDir, Dictionary<string, DateTime> fileTimestamps)
    {
        if (!items.TryGetProperty(itemType, out var itemArray) || itemArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in itemArray.EnumerateArray())
        {
            if (item.TryGetProperty("Identity", out var identity))
            {
                var relativePath = identity.GetString();
                if (!string.IsNullOrEmpty(relativePath))
                {
                    var fullPath = Path.GetFullPath(relativePath, projectDir);
                    RecordFileTimestamp(fileTimestamps, fullPath);
                }
            }
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
