// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Minimalistic reporter that does nothing.
/// </summary>
[Experimental("ASPIREPUBLISHERS001")]
public sealed class NullPublishingActivityProgressReporter : IPublishingActivityProgressReporter
{
    /// <inheritdoc/>
    public Task<PublishingActivity> CreateActivityAsync(string id, string initialStatusText, bool isPrimary, CancellationToken cancellationToken)
    {
        var activity = new PublishingActivity(id, isPrimary);
        activity.LastStatus = new PublishingActivityStatus
        {
            Activity = activity,
            StatusText = initialStatusText,
            IsComplete = false,
            IsError = false
        };

        return Task.FromResult(activity);
    }

    /// <inheritdoc/>
    public Task UpdateActivityStatusAsync(PublishingActivity publishingActivity, Func<PublishingActivityStatus, PublishingActivityStatus> statusUpdate, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
