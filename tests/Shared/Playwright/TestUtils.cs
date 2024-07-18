// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Workload.Tests;

public static class TestUtils
{
    public static DirectoryInfo? FindRepoRoot()
    {
        DirectoryInfo? repoRoot = new(AppContext.BaseDirectory);
        while (repoRoot != null)
        {
            // To support git worktrees, check for either a directory or a file named ".git"
            if (Directory.Exists(Path.Combine(repoRoot.FullName, ".git")) || File.Exists(Path.Combine(repoRoot.FullName, ".git")))
            {
                return repoRoot;
            }

            repoRoot = repoRoot.Parent;
        }

        return null;
    }
}
