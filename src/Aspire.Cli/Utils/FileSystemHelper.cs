// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Helper class for file system operations.
/// </summary>
internal static class FileSystemHelper
{
    /// <summary>
    /// Copies an entire directory and its contents to a new location.
    /// </summary>
    /// <param name="sourceDir">The source directory to copy from.</param>
    /// <param name="destinationDir">The destination directory to copy to.</param>
    /// <exception cref="ArgumentNullException">Thrown when sourceDir or destinationDir is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the source directory does not exist.</exception>
    internal static void CopyDirectory(string sourceDir, string destinationDir)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceDir);
        ArgumentException.ThrowIfNullOrEmpty(destinationDir);

        var sourceDirInfo = new DirectoryInfo(sourceDir);
        if (!sourceDirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        // Create the destination directory if it doesn't exist
        Directory.CreateDirectory(destinationDir);

        // Copy all files in the current directory
        foreach (var file in sourceDirInfo.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: false);
        }

        // Recursively copy all subdirectories
        foreach (var subDir in sourceDirInfo.GetDirectories())
        {
            var targetSubDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, targetSubDir);
        }
    }
}
