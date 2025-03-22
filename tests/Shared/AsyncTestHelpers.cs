// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.InternalTesting;

internal static class TestConstants
{
    // IMPORTANT: If a test fails because these time out, consider adding a new field with a larger value.
    // These values are as big as they need to be to test things complete in the expected time.
#if DEBUG
    // Shorter duration when running tests with debug.
    // Less time waiting for hang unit tests to fail in aspnetcore solution.
    public const int DefaultTimeoutDuration = 5 * 1000;
    public const int LongTimeoutDuration = 60 * 1000;
    public const int DefaultOrchestratorTestTimeout = 15 * 1000;
    public const int DefaultOrchestratorTestLongTimeout = 45 * 1000;
#else
    public const int DefaultTimeoutDuration = 30 * 1000;
    public const int LongTimeoutDuration = 120 * 1000;
    public const int DefaultOrchestratorTestTimeout = DefaultTimeoutDuration;
    public const int DefaultOrchestratorTestLongTimeout = LongTimeoutDuration;
#endif

    public static TimeSpan DefaultTimeoutTimeSpan { get; } = TimeSpan.FromMilliseconds(DefaultTimeoutDuration);
    public static TimeSpan LongTimeoutTimeSpan { get; } = TimeSpan.FromMilliseconds(LongTimeoutDuration);
}

internal static class AsyncTestHelpers
{
    private static readonly string s_assemblyName = typeof(TimeoutException).Assembly.GetName().Name!;

    public static CancellationTokenSource CreateDefaultTimeoutTokenSource(int milliseconds = TestConstants.DefaultTimeoutDuration)
    {
        var cts = new CancellationTokenSource();
        if (!Debugger.IsAttached)
        {
            cts.CancelAfter(TimeSpan.FromMilliseconds(milliseconds));
        }
        return cts;
    }

    public static async IAsyncEnumerable<T> DefaultTimeout<T>(this IAsyncEnumerable<T> asyncEnumerable, int milliseconds = TestConstants.DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        // Wrap the enumerable with an enumerable that times out after exceeding time limit on each iteration.
        await using var enumator = asyncEnumerable.GetAsyncEnumerator();
        while (await enumator.MoveNextAsync().DefaultTimeout(milliseconds, filePath, lineNumber))
        {
            yield return enumator.Current;
        }
    }

    public static Task DefaultTimeout(this Task task, int milliseconds = TestConstants.DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
    }

    public static Task DefaultTimeout(this Task task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.TimeoutAfter(timeout, filePath, lineNumber);
    }

    public static Task DefaultTimeout(this ValueTask task, int milliseconds = TestConstants.DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.AsTask().TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
    }

    public static Task DefaultTimeout(this ValueTask task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.AsTask().TimeoutAfter(timeout, filePath, lineNumber);
    }

    public static Task<T> DefaultTimeout<T>(this Task<T> task, int milliseconds = TestConstants.DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
    }

    public static Task<T> DefaultTimeout<T>(this Task<T> task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.TimeoutAfter(timeout, filePath, lineNumber);
    }

    public static Task<T> DefaultTimeout<T>(this ValueTask<T> task, int milliseconds = TestConstants.DefaultTimeoutDuration, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.AsTask().TimeoutAfter(TimeSpan.FromMilliseconds(milliseconds), filePath, lineNumber);
    }

    public static Task<T> DefaultTimeout<T>(this ValueTask<T> task, TimeSpan timeout, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = default)
    {
        return task.AsTask().TimeoutAfter(timeout, filePath, lineNumber);
    }

    public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        // Don't create a timer if the task is already completed
        // or the debugger is attached
        if (task.IsCompleted || Debugger.IsAttached)
        {
            return await task.ConfigureAwait(false);
        }
#if NET6_0_OR_GREATER
        try
        {
            return await task.WaitAsync(timeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex) when (ex.Source == s_assemblyName)
        {
            throw new TimeoutException(CreateMessage(timeout, filePath!, lineNumber));
        }
#else
        var cts = new CancellationTokenSource();
        if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false))
        {
            cts.Cancel();
            return await task.ConfigureAwait(false);
        }
        else
        {
            throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
        }
#endif
    }

    public static async Task TimeoutAfter(this Task task, TimeSpan timeout,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = default)
    {
        // Don't create a timer if the task is already completed
        // or the debugger is attached
        if (task.IsCompleted || Debugger.IsAttached)
        {
            await task.ConfigureAwait(false);
            return;
        }
#if NET6_0_OR_GREATER
        try
        {
            await task.WaitAsync(timeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex) when (ex.Source == s_assemblyName)
        {
            throw new TimeoutException(CreateMessage(timeout, filePath!, lineNumber));
        }
#else
        var cts = new CancellationTokenSource();
        if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false))
        {
            cts.Cancel();
            await task.ConfigureAwait(false);
        }
        else
        {
            throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
        }
#endif
    }

    private static string CreateMessage(TimeSpan timeout, string filePath, int lineNumber)
        => string.IsNullOrEmpty(filePath)
        ? $"The operation timed out after reaching the limit of {timeout.TotalMilliseconds}ms."
        : $"The operation at {filePath}:{lineNumber} timed out after reaching the limit of {timeout.TotalMilliseconds}ms.";

    public static Task AssertIsTrueRetryAsync(Func<bool> assert, string message, ILogger? logger = null)
    {
        return AssertIsTrueRetryAsync(() => Task.FromResult(assert()), message, logger);
    }

    public static async Task AssertIsTrueRetryAsync(Func<Task<bool>> assert, string message, ILogger? logger = null)
    {
        const int Retries = 10;

        logger?.LogInformation("Start: " + message);

        for (var i = 0; i < Retries; i++)
        {
            if (i > 0)
            {
                await Task.Delay((i + 1) * (i + 1) * 10 * 5);
            }

            if (await assert())
            {
                logger?.LogInformation("End: " + message);
                return;
            }
        }

        throw new InvalidOperationException($"Assert failed after {Retries} retries: {message}");
    }
}
