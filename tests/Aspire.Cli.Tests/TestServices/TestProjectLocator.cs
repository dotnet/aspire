// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestProjectLocator : IProjectLocator
{
    public Func<FileInfo?, FileInfo?>? UseOrFindAppHostProjectFileCallback { get; set; }

    public FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
    {
        if (UseOrFindAppHostProjectFileCallback != null)
        {
            return UseOrFindAppHostProjectFileCallback(projectFile);
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

