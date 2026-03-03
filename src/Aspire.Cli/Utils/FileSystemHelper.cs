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
    /// <param name="overwrite">Whether to overwrite existing files in the destination directory.</param>
    internal static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceDir);
        ArgumentException.ThrowIfNullOrEmpty(destinationDir);

        var sourceDirInfo = new DirectoryInfo(sourceDir);
        if (!sourceDirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        // Use a stack to avoid recursion and potential stack overflow with deep directory structures
        var stack = new Stack<(DirectoryInfo Source, string Destination)>();
        stack.Push((sourceDirInfo, destinationDir));

        while (stack.Count > 0)
        {
            var (currentSource, currentDestination) = stack.Pop();

            // Create the destination directory if it doesn't exist
            Directory.CreateDirectory(currentDestination);

            // Copy all files in the current directory
            foreach (var file in currentSource.GetFiles())
            {
                var targetFilePath = Path.Combine(currentDestination, file.Name);
                file.CopyTo(targetFilePath, overwrite);
            }

            // Push all subdirectories onto the stack
            foreach (var subDir in currentSource.GetDirectories())
            {
                var targetSubDir = Path.Combine(currentDestination, subDir.Name);
                stack.Push((subDir, targetSubDir));
            }
        }
    }
}
