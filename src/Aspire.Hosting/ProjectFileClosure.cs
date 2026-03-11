// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Represents the set of files that a project depends on for compilation.
/// </summary>
internal sealed class ProjectFileClosure
{
    /// <summary>
    /// Gets the paths and last-write timestamps of all files in the closure.
    /// </summary>
    public required Dictionary<string, DateTime> FileTimestamps { get; init; }

    /// <summary>
    /// Gets the project path this closure is for.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Gets the time this closure was captured.
    /// </summary>
    public required DateTime CapturedAt { get; init; }

    /// <summary>
    /// Checks whether any files in the closure have been modified since the closure was captured.
    /// </summary>
    /// <returns>A list of changed file paths, or empty if nothing changed.</returns>
    public List<string> GetChangedFiles()
    {
        var changedFiles = new List<string>();

        foreach (var (filePath, lastWriteTime) in FileTimestamps)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var currentWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (currentWriteTime > lastWriteTime)
                    {
                        changedFiles.Add(filePath);
                    }
                }
            }
            catch (IOException)
            {
                // File may be locked or inaccessible; skip it.
            }
        }

        return changedFiles;
    }
}
