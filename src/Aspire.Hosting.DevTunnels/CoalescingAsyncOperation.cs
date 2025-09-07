// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Provides a reusable pattern for running an asynchronous operation ensuring that only one
/// instance executes concurrently while allowing many callers to await its completion.
/// Subsequent callers while the operation is in-flight coalesce onto the same task. When the
/// operation completes (success, fault, or cancellation) a future caller will start a new execution.
/// </summary>
internal abstract class CoalescingAsyncOperation : IDisposable
{
    private readonly object _sync = new();
    private Task? _runningTask;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Implement the core asynchronous operation logic. Implementations should throw if they fail.
    /// </summary>
    /// <param name="cancellationToken">Token signaled when the initial caller's token is cancelled or the instance disposed.</param>
    protected abstract Task ExecuteCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ensures that the core operation is running. Only one execution is active at once; if already
    /// running this returns a task that completes when the in-flight operation finishes. If not running
    /// a new execution starts.
    /// </summary>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        Task? current;
        lock (_sync)
        {
            if (_runningTask is { IsCompleted: false })
            {
                current = _runningTask;
            }
            else
            {
                _cts?.Dispose();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                current = _runningTask = ExecuteWrapperAsync(_cts.Token);

                _ = _runningTask.ContinueWith(t =>
                {
                    lock (_sync)
                    {
                        if (ReferenceEquals(t, _runningTask))
                        {
                            _runningTask = null;
                        }
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        // Each caller can independently cancel their wait without cancelling the shared operation.
        return current.WaitAsync(cancellationToken);
    }

    private async Task ExecuteWrapperAsync(CancellationToken ct)
    {
        await ExecuteCoreAsync(ct).ConfigureAwait(false);
    }

    public virtual void Dispose()
    {
        lock (_sync)
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
                // ignored
            }
            _cts?.Dispose();
            _cts = null;
            _runningTask = null;
        }
    }
}
