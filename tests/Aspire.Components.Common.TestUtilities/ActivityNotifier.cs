// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using OpenTelemetry;

namespace Aspire.Components.Common.TestUtilities;

/// <summary>
/// An OpenTelemetry processor that can notify callers when it has processed an Activity.
/// </summary>
public sealed class ActivityNotifier : BaseProcessor<Activity>
{
    private readonly Channel<Activity> _activityChannel = Channel.CreateUnbounded<Activity>();

    public async Task<List<Activity>> TakeAsync(int count, TimeSpan timeout)
    {
        var activityList = new List<Activity>();
        using var cts = new CancellationTokenSource(timeout);
        await foreach (var activity in WaitAsync(cts.Token))
        {
            activityList.Add(activity);
            if (activityList.Count == count)
            {
                break;
            }
        }

        return activityList;
    }

    public override void OnEnd(Activity data)
    {
        _activityChannel.Writer.TryWrite(data);
    }

    private async IAsyncEnumerable<Activity> WaitAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var activity in _activityChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return activity;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activityChannel.Writer.TryComplete();
        }

        base.Dispose(disposing);
    }
}
