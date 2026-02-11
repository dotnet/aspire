// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;

namespace Aspire.Cli.Utils;

internal static class AspireRepositoryDetector
{
    private const string AspireSolutionFileName = "Aspire.slnx";

    public static string? DetectRepositoryRoot(string? startPath = null)
    {
        var repoRoot = FindRepositoryRoot(startPath);
        if (!string.IsNullOrEmpty(repoRoot))
        {
            return repoRoot;
        }

        var envRoot = Environment.GetEnvironmentVariable(BundleDiscovery.RepoRootEnvVar);
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot))
        {
            return Path.GetFullPath(envRoot);
        }

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            repoRoot = FindRepositoryRoot(Path.GetDirectoryName(processPath));
            if (!string.IsNullOrEmpty(repoRoot))
            {
                return repoRoot;
            }
        }

        return null;
    }

    private static string? FindRepositoryRoot(string? startPath)
    {
        if (string.IsNullOrEmpty(startPath))
        {
            return null;
        }

        var currentDirectory = ResolveSearchDirectory(startPath);
        while (!string.IsNullOrEmpty(currentDirectory))
        {
            if (File.Exists(Path.Combine(currentDirectory, AspireSolutionFileName)))
            {
                return currentDirectory;
            }

            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }

        return null;
    }

    private static string ResolveSearchDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            return fullPath;
        }

        if (File.Exists(fullPath))
        {
            return Path.GetDirectoryName(fullPath)!;
        }

        var parentDirectory = Path.GetDirectoryName(fullPath);
        return string.IsNullOrEmpty(parentDirectory) ? fullPath : parentDirectory;
    }
}
