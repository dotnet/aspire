// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestProjectLocator : IProjectLocator
{
    public Func<FileInfo?, CancellationToken, Task<FileInfo?>>? UseOrFindAppHostProjectFileAsyncCallback { get; set; }
    public Func<FileInfo?, MultipleAppHostProjectsFoundBehavior, CancellationToken, Task<AppHostProjectSearchResult>>? UseOrFindAppHostProjectFileWithBehaviorAsyncCallback { get; set; }

    public async Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, CancellationToken cancellationToken = default)
    {
        if (UseOrFindAppHostProjectFileWithBehaviorAsyncCallback != null)
        {
            return await UseOrFindAppHostProjectFileWithBehaviorAsyncCallback(projectFile, multipleAppHostProjectsFoundBehavior, cancellationToken);
        }

        // Fallback behavior
        var appHostFile = await UseOrFindAppHostProjectFileAsync(projectFile, cancellationToken);
        if (appHostFile is null)
        {
            return new AppHostProjectSearchResult(null, []);
        }

        return new AppHostProjectSearchResult(appHostFile, [appHostFile]);
    }

    public async Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
    {
        if (UseOrFindAppHostProjectFileAsyncCallback != null)
        {
            return await UseOrFindAppHostProjectFileAsyncCallback(projectFile, cancellationToken);
        }

        // Fallback behavior if not overridden.
        if (projectFile != null)
        {
            return projectFile;
        }

        var fakeProjectFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "AppHost.csproj");
        return new FileInfo(fakeProjectFilePath);
    }
}

