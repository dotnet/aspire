// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestProjectLocator : IProjectLocator
{
    public Func<FileInfo?, bool, CancellationToken, Task<FileInfo?>>? UseOrFindAppHostProjectFileAsyncCallback { get; set; }

    public Func<FileInfo?, MultipleAppHostProjectsFoundBehavior, bool, CancellationToken, Task<AppHostProjectSearchResult>>? UseOrFindAppHostProjectFileWithBehaviorAsyncCallback { get; set; }

    public Func<string, CancellationToken, Task<IReadOnlyList<FileInfo>>>? FindExecutableProjectsAsyncCallback { get; set; }

    public async Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, bool createSettingsFile, CancellationToken cancellationToken)
    {
        if (UseOrFindAppHostProjectFileAsyncCallback != null)
        {
            return await UseOrFindAppHostProjectFileAsyncCallback(projectFile, createSettingsFile, cancellationToken);
        }

        // Fallback behavior if not overridden.
        if (projectFile != null)
        {
            return projectFile;
        }

        var fakeProjectFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "AppHost.csproj");
        return new FileInfo(fakeProjectFilePath);
    }

    public async Task<AppHostProjectSearchResult> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, MultipleAppHostProjectsFoundBehavior multipleAppHostProjectsFoundBehavior, bool createSettingsFile, CancellationToken cancellationToken = default)
    {
        if (UseOrFindAppHostProjectFileWithBehaviorAsyncCallback != null)
        {
            return await UseOrFindAppHostProjectFileWithBehaviorAsyncCallback(projectFile, multipleAppHostProjectsFoundBehavior, createSettingsFile, cancellationToken);
        }

        // Fallback behavior
        var appHostFile = await UseOrFindAppHostProjectFileAsync(projectFile, createSettingsFile, cancellationToken);
        if (appHostFile is null)
        {
            return new AppHostProjectSearchResult(null, []);
        }

        return new AppHostProjectSearchResult(appHostFile, [appHostFile]);
    }

    public Task<IReadOnlyList<FileInfo>> FindExecutableProjectsAsync(string searchDirectory, CancellationToken cancellationToken)
    {
        if (FindExecutableProjectsAsyncCallback != null)
        {
            return FindExecutableProjectsAsyncCallback(searchDirectory, cancellationToken);
        }

        // Fallback behavior if not overridden.
        var fakeProjectFilePath = Path.Combine(searchDirectory, "SomeExecutable.csproj");
        return Task.FromResult<IReadOnlyList<FileInfo>>(new List<FileInfo> { new FileInfo(fakeProjectFilePath) });
    }
}

