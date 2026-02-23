// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Git;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestGitRepository : IGitRepository
{
    public Func<CancellationToken, Task<DirectoryInfo?>>? GetRootAsyncCallback { get; set; }

    public async Task<DirectoryInfo?> GetRootAsync(CancellationToken cancellationToken)
    {
        if (GetRootAsyncCallback is not null)
        {
            return await GetRootAsyncCallback(cancellationToken);
        }

        // Default behavior: return null (not in a git repository)
        return null;
    }
}
