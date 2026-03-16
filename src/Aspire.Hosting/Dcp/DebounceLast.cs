// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp;

/// <summary>
/// Calls a runner function after a specified delay, coalescing multiple concurrent calls into one.
/// If new calls arrive during the delay, the execution is postponed further, but no more than <paramref name="maxDelay"/>.
/// All callers within a debounce cycle receive the same result.
/// A new debounce cycle starts just before the runner is invoked, so that any calls arriving
/// while the runner is executing will trigger a subsequent run.
/// </summary>
internal sealed class DebounceLast<TResult>(
    Func<CancellationToken, Task<TResult>> runner,
    TimeSpan delay,
    TimeSpan maxDelay,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly object _lock = new();
    private TaskCompletionSource<TResult>? _completion;
    private List<CancellationToken>? _callerTokens;
    private DateTimeOffset _fireAt;
    private DateTimeOffset _threshold;

    private static readonly TimeSpan s_microsecond = TimeSpan.FromMicroseconds(1);

    /// <summary>
    /// Submits a call for debounced execution. The actual runner is invoked after the debounce
    /// delay elapses without new calls, or once the maximum delay is reached.
    /// The runner receives a cancellation token that is cancelled when any caller's
    /// <paramref name="cancellationToken"/> within the debounce cycle is cancelled.
    /// </summary>
    public Task<TResult> RunAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<TResult> completion;

        lock (_lock)
        {
            if (_completion is null)
            {
                // Start a new debounce cycle.
                var now = _clock.GetUtcNow();
                _completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                _callerTokens = [];
                _fireAt = now + delay;
                _threshold = now + maxDelay;
                completion = _completion;

                AddCallerToken(cancellationToken);

                _ = WaitAndExecuteAsync(completion);
            }
            else
            {
                // Debounce cycle already in progress; extend the delay if within the max-delay threshold.
                completion = _completion;
                var proposed = _clock.GetUtcNow() + delay;
                if (proposed < _threshold)
                {
                    _fireAt = proposed;
                }

                AddCallerToken(cancellationToken);
            }
        }

        return completion.Task;
    }

    private void AddCallerToken(CancellationToken cancellationToken)
    {
        if (cancellationToken.CanBeCanceled)
        {
            _callerTokens!.Add(cancellationToken);
        }
    }

    private async Task WaitAndExecuteAsync(
        TaskCompletionSource<TResult> completion)
    {
        CancellationTokenSource? linkedCts = null;
        var refreshLinkedCts = () =>
        {
            linkedCts?.Dispose();
            linkedCts = _callerTokens is { Count: > 0 }
                ? CancellationTokenSource.CreateLinkedTokenSource([.. _callerTokens])
                : null;
        };
        var effectiveToken = () => linkedCts?.Token ?? CancellationToken.None;

        try
        {
            // Wait for the debounce delay, re-checking in case it was extended by additional callers.
            while (true)
            {
                DateTimeOffset fireAt;
                lock (_lock)
                {
                    fireAt = _fireAt;
                    refreshLinkedCts();
                }

                var remaining = fireAt - _clock.GetUtcNow();

                // Compare against very small TimeSpan, but not zero, to avoid issues with exact arithmetic with FakeTimeProvider (in tests).
                if (remaining <= s_microsecond)
                {
                    break;
                }

                await Task.Delay(remaining, _clock, effectiveToken()).ConfigureAwait(false);
            }

            // Clear the current cycle BEFORE invoking the runner so that any RunAsync() calls arriving while the runner is executing 
            // will start a new debounce cycle instead of waiting on this one.
            lock (_lock)
            {
                refreshLinkedCts();
                _completion = null;
                _callerTokens = null;
            }

            var result = await runner(effectiveToken()).ConfigureAwait(false);
            completion.SetResult(result);
        }
        catch (OperationCanceledException ex) when (linkedCts is not null && linkedCts.IsCancellationRequested)
        {
            ClearCompletionIfCurrent(completion);
            completion.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            ClearCompletionIfCurrent(completion);
            completion.TrySetException(ex);
        }
        finally
        {
            linkedCts?.Dispose();
        }
    }

    private void ClearCompletionIfCurrent(TaskCompletionSource<TResult> completion)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_completion, completion))
            {
                _completion = null;
                _callerTokens = null;
            }
        }
    }
}
