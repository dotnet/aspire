// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Time.Testing;

namespace Aspire.Hosting.Tests.Utils;

internal sealed class SignalingFakeTimeProvider(TimeSpan signaledDueTime) : FakeTimeProvider
{
    private readonly TaskCompletionSource _timerCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task TimerCreated => _timerCreated.Task;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        if (dueTime == signaledDueTime)
        {
            // Signal after registration so advancing time cannot race with scheduling the delay.
            _timerCreated.TrySetResult();
        }

        return timer;
    }
}
