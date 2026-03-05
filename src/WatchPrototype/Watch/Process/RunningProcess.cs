// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

internal sealed class RunningProcess(
    int id,
    Task<int> task,
    CancellationTokenSource exitedSource,
    CancellationTokenSource terminationSource) : IAsyncDisposable
{
    private CancellationTokenSource? _terminationSource = terminationSource;

    /// <summary>
    /// Cancellation token triggered when the process exits.
    /// Stores the token to allow callers to use the token even after the source has been disposed.
    /// </summary>
    public readonly CancellationToken ExitedCancellationToken = exitedSource.Token;

    public Task<int> Task => task;
    public int Id => id;

    ValueTask IAsyncDisposable.DisposeAsync()
        => DisposeAsync(isExiting: false);

    public async ValueTask DisposeAsync(bool isExiting)
    {
        var terminationSource = Interlocked.Exchange(ref _terminationSource, null);
        ObjectDisposedException.ThrowIf(terminationSource == null, this);

        // do not await process termination since it's already in progress:
        if (!isExiting)
        {
            terminationSource.Cancel();
            await task;
        }

        terminationSource.Dispose();

        exitedSource.Cancel();
        exitedSource.Dispose();
    }

    /// <summary>
    /// Terminates the process if it hasn't terminated yet.
    /// Awating the task triggers OnExit handlers, which in turn call <see cref="DisposeAsync"/>.
    /// </summary>
    public Task TerminateAsync()
    {
        _terminationSource?.Cancel();
        return task;
    }
}
