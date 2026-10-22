// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Cli.Git;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestGitRunner : IGitRunner
{
    public Func<string, DirectoryInfo, CancellationToken, Task> CloneAsyncCallback { get; set; } =
        (_, _, _) => throw new InvalidOperationException("Unexpected Git clone invocation.");

    public ConcurrentQueue<(string Repository, DirectoryInfo Destination)> CloneCalls { get; } = new();

    public Task CloneAsync(string repository, DirectoryInfo destination, CancellationToken cancellationToken)
    {
        CloneCalls.Enqueue((repository, destination));
        return CloneAsyncCallback(repository, destination, cancellationToken);
    }
}
