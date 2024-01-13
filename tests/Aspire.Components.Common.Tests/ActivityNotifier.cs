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
    private readonly TaskCompletionSource _taskSource = new TaskCompletionSource();

    public Task ActivityReceived => _taskSource.Task;

    public List<Activity> ExportedActivities { get; } = [];

    public override void OnEnd(Activity data)
    {
        ExportedActivities.Add(data);
        _taskSource.SetResult();
    }
}
