// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;

namespace Aspire.Components.Common.Tests;

/// <summary>
/// An OpenTelemetry processor that can notify callers when it has processed an Activity.
/// </summary>
public sealed class ActivityNotifier : BaseProcessor<Activity>
{
    // RunContinuationsAsynchronously because OnEnd gets invoked on the thread creating the Activity.
    // Running more test code on this thread can cause deadlocks in the case where the Activity is created on a "drain thread" (ex. Redis)
    // and the test method disposes the Host on that same thread. Disposing the Host will dispose the Instrumentation, which tries joining
    // with the "drain thread".
    private readonly TaskCompletionSource _taskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ActivityReceived => _taskSource.Task;

    public List<Activity> ExportedActivities { get; } = [];

    public override void OnEnd(Activity data)
    {
        ExportedActivities.Add(data);
        _taskSource.SetResult();
    }
}
