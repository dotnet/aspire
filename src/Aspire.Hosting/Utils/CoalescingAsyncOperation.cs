// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// Provides a reusable pattern for running an asynchronous operation ensuring that only one
/// instance executes concurrently while allowing many callers to await its completion.
/// Subsequent callers while the operation is in-flight coalesce onto the same task. When the
/// operation completes (success, fault, or cancellation) a future caller will start a new execution.
/// </summary>
internal abstract class CoalescingAsyncOperation : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
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
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Task current;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_runningTask is { IsCompleted: false })
            {
                // Already running, coalesce onto the existing task
                current = _runningTask;
            }
            else
            {
                // Start a new execution
                _cts?.Dispose();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                current = _runningTask = ExecuteWrapperAsync(_cts.Token);

                _ = _runningTask.ContinueWith(static (t, state) =>
                {
                    var self = (CoalescingAsyncOperation)state!;
                    self.ClearCompleted(t);
                }, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
        finally
        {
            _gate.Release();
        }

        await current.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteWrapperAsync(CancellationToken ct) => await ExecuteCoreAsync(ct).ConfigureAwait(false);

    private void ClearCompleted(Task completed)
    {
        // Fire-and-forget async cleanup (no need to await where called).
        _ = ClearCompletedAsync(completed);
    }

    private async Task ClearCompletedAsync(Task completed)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (ReferenceEquals(completed, _runningTask))
            {
                _runningTask = null; // Allow GC of completed task.
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public virtual void Dispose()
    {
        _gate.Wait();
        try
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
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
