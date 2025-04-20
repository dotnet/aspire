// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Git;

internal interface IGitRootLocator
{
    DirectoryInfo? FindGitRoot(DirectoryInfo startDirectory);
}

internal sealed class GitRootLocator(ILogger<GitRootLocator> logger) : IGitRootLocator
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(GitRootLocator));

    public DirectoryInfo? FindGitRoot(DirectoryInfo startDirectory)
    {
        using var activity = _activitySource.StartActivity();

        logger.LogTrace("Starting search for Git root from directory: {Directory}", startDirectory.FullName);

        var currentDirectory = startDirectory;

        while (currentDirectory != null)
        {
            logger.LogTrace("Checking directory: {Directory}", currentDirectory.FullName);

            var gitFolder = new DirectoryInfo(Path.Combine(currentDirectory.FullName, ".git"));
            if (gitFolder.Exists)
            {
                logger.LogTrace("Found Git root at directory: {Directory}", currentDirectory.FullName);
                return currentDirectory;
            }

            currentDirectory = currentDirectory.Parent;
        }

        logger.LogTrace("No Git root found starting from directory: {Directory}", startDirectory.FullName);
        return null;
    }
}