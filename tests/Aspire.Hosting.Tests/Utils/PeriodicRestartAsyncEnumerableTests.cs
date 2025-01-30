// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests.Utils;

public class PeriodicRestartAsyncEnumerableTests
{
    [Fact]
    public async Task CancellingMainTokenCancelsEnumerable()
    {
        using var cts = new CancellationTokenSource();
        var enumerable = new PeriodicRestartAsyncEnumerable<int>((cancellationToken) => Task.FromResult(RepeatingAsyncEnumerable(1, TimeSpan.FromMilliseconds(10), cancellationToken)), TimeSpan.FromMilliseconds(750));
        cts.CancelAfter(TimeSpan.FromMilliseconds(250));
        var start = DateTime.UtcNow;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in enumerable.WithCancellation(cts.Token))
            {
                if (DateTime.UtcNow - start > TimeSpan.FromMilliseconds(500))
                {
                    Assert.Fail("expected cancellation before 500ms");
                }
            }
        });
    }

    private static int s_totalEnumerablesRun;
    private static int s_activeRunningEnumerables;

    [Fact]
    public async Task EnumerableIsRecreatedPeriodically()
    {
        using var cts = new CancellationTokenSource();
        var enumerable = new PeriodicRestartAsyncEnumerable<int>((cancellationToken) => Task.FromResult(CountingAsyncEnumerable(1, TimeSpan.FromMilliseconds(10), cancellationToken)), TimeSpan.FromMilliseconds(100));
        cts.CancelAfter(TimeSpan.FromMilliseconds(300));
        var start = DateTime.UtcNow;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in enumerable.WithCancellation(cts.Token))
            {
                if (DateTime.UtcNow - start > TimeSpan.FromMilliseconds(500))
                {
                    Assert.Fail("expected cancellation before 500ms");
                }
            }
        });

        Assert.True(s_totalEnumerablesRun > 1, "expected additional iteration runs");
        Assert.True(s_activeRunningEnumerables == 0, "expected all enumerables to be ended after cancellation");
    }

    static async IAsyncEnumerable<int> RepeatingAsyncEnumerable(int value, TimeSpan delay, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return value;
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    static async IAsyncEnumerable<int> CountingAsyncEnumerable(int value, TimeSpan delay, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref s_totalEnumerablesRun);
        Interlocked.Increment(ref s_activeRunningEnumerables);

        try
        {
            await foreach (var innerValue in RepeatingAsyncEnumerable(value, delay, cancellationToken))
            {
                yield return innerValue;
            }
        }
        finally
        {
            Interlocked.Decrement(ref s_activeRunningEnumerables);
        }
    }
}
