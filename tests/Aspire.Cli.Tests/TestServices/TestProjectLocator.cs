// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestProjectLocator : IProjectLocator
{
    public Func<FileInfo?, CancellationToken, Task<FileInfo?>>? UseOrFindAppHostProjectFileAsyncCallback { get; set; }

    public Func<string, CancellationToken, Task<List<FileInfo>>>? FindAppHostProjectFilesAsyncCallback { get; set; }

    public Func<string, CancellationToken, Task<IReadOnlyList<FileInfo>>>? FindExecutableProjectsAsyncCallback { get; set; }

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

    public Task<List<FileInfo>> FindAppHostProjectFilesAsync(string searchDirectory, CancellationToken cancellationToken)
    {
        if (FindAppHostProjectFilesAsyncCallback != null)
        {
            return FindAppHostProjectFilesAsyncCallback(searchDirectory, cancellationToken);
        }

        // Fallback behavior if not overridden.
        var fakeProjectFilePath = Path.Combine(searchDirectory, "AppHost.csproj");
        return Task.FromResult(new List<FileInfo> { new FileInfo(fakeProjectFilePath) });
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

