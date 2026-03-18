// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// Calls a runner function after a specified delay, coalescing multiple concurrent calls into one.
/// If new calls arrive during the delay, the execution is postponed further, but no more than the maximum delay.
/// All callers within a debounce cycle receive the same result.
/// A new debounce cycle starts just before the runner is invoked, so that any calls arriving
/// while the runner is executing will trigger a subsequent run.
/// </summary>
internal sealed class DebounceLast<TResult>
{
    private readonly TimeProvider _clock;
    private readonly object _lock = new();
    private TaskCompletionSource<TResult>? _completion;
    private DateTimeOffset _fireAt;
    private DateTimeOffset _threshold;
    private readonly Func<Task<TResult>> _runner;
    private readonly TimeSpan _delay;
    private readonly TimeSpan _maxDelay;

    // Treat TimeSpan less than 1 us as zero-length TimeSpan to avoid issues with Task.Delay granularity and clock precision. 
    private static readonly TimeSpan s_microsecond = TimeSpan.FromMicroseconds(1);

    public DebounceLast(
        Func<Task<TResult>> runner,
        TimeSpan delay,
        TimeSpan maxDelay,
        TimeProvider? clock = null)
    {
        _clock = clock ?? TimeProvider.System;
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, s_microsecond, nameof(delay));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, s_microsecond, nameof(maxDelay));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, delay, nameof(maxDelay));
        _delay = delay;
        _maxDelay = maxDelay;
    }

    /// <summary>
    /// Submits a call for debounced execution. The actual runner is invoked after the debounce
    /// delay elapses without new calls, or once the maximum delay is reached.
    /// </summary>
    public Task<TResult> RunAsync()
    {
        TaskCompletionSource<TResult> completion;

        lock (_lock)
        {
            if (_completion is null)
            {
                // Start a new debounce cycle.
                var now = _clock.GetUtcNow();
                _completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                _fireAt = now + _delay;
                _threshold = now + _maxDelay;
                completion = _completion;

                _ = WaitAndExecuteAsync(completion);
            }
            else
            {
                // Debounce cycle already in progress; extend the delay if within the max-delay threshold.
                completion = _completion;
                var proposed = _clock.GetUtcNow() + _delay;
                if (proposed < _threshold)
                {
                    _fireAt = proposed;
                }
            }
        }

        return completion.Task;
    }

    private async Task WaitAndExecuteAsync(
        TaskCompletionSource<TResult> completion)
    {
        try
        {
            // Wait for the debounce delay, re-checking in case it was extended by additional callers.
            while (true)
            {
                DateTimeOffset fireAt;
                lock (_lock)
                {
                    fireAt = _fireAt;
                }

                var remaining = fireAt - _clock.GetUtcNow();

                if (remaining <= s_microsecond)
                {
                    break;
                }

                await Task.Delay(remaining, _clock).ConfigureAwait(false);
            }

            // Clear the current cycle BEFORE invoking the runner so that any RunAsync() calls arriving while the runner is executing 
            // will start a new debounce cycle instead of waiting on this one.
            lock (_lock)
            {
                _completion = null;
            }

            var result = await _runner().ConfigureAwait(false);
            completion.SetResult(result);
        }
        catch (Exception ex)
        {
            ClearCompletionIfCurrent(completion);
            completion.TrySetException(ex);
        }
    }

    private void ClearCompletionIfCurrent(TaskCompletionSource<TResult> completion)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_completion, completion))
            {
                _completion = null;
            }
        }
    }
}
