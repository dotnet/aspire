// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Git;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Cli.Tests.Git;

public class GitRootLocatorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void FindGitRootReturnsNullIfNoGitRootFound()
    {
        var logger = new NullLogger<GitRootLocator>();

        // Create a repo but don't initialize it.
        using var tempRepo = TemporaryRepo.Create(outputHelper);

        var gitRootLocator = new GitRootLocator(logger);

        var result = gitRootLocator.FindGitRoot(tempRepo.RootDirectory);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindGitRootReturnsRepoRootWhenSearchStartsInRepoRoot()
    {
        var logger = new NullLogger<GitRootLocator>();

        // Create a repo but don't initialize it.
        using var tempRepo = TemporaryRepo.Create(outputHelper);
        await tempRepo.InitializeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        var gitRootLocator = new GitRootLocator(logger);

        var result = gitRootLocator.FindGitRoot(tempRepo.RootDirectory);

        Assert.Equal(tempRepo.RootDirectory.FullName, result?.FullName);
    }

    [Fact]
    public async Task FindGitRootReturnsRepoRootWhenSearchStartsInNestedDirectories()
    {
        var logger = new NullLogger<GitRootLocator>();

        // Create a repo but don't initialize it.
        using var tempRepo = TemporaryRepo.Create(outputHelper);
        await tempRepo.InitializeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        var dir1 = tempRepo.CreateDirectory("dir1");
        var dir2 = dir1.CreateSubdirectory("dir2");
        var dir3 = dir2.CreateSubdirectory("dir3");

        var gitRootLocator = new GitRootLocator(logger);

        var result = gitRootLocator.FindGitRoot(dir3);

        Assert.Equal(tempRepo.RootDirectory.FullName, result?.FullName);
    }
}