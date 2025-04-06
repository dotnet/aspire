// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents a publishing activity.
/// </summary>
[Experimental("ASPIREPUBLISHERS001")]
public sealed class PublishingActivity
{
    internal PublishingActivity(string id, bool isPrimary = false)
    {
        Id = id;
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// Unique Id of the publishing activity.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// Indicates whether the publishing activity is the primary activity.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// The status text of the publishing activity.
    /// </summary>
    public PublishingActivityStatus? LastStatus { get; internal set; }
}

/// <summary>
/// Represents the status of a publishing activity.
/// </summary>
[Experimental("ASPIREPUBLISHERS001")]
public sealed record PublishingActivityStatus
{
    /// <summary>
    /// The publishing activity associated with this status.
    /// </summary>
    public required PublishingActivity Activity { get; init; }

    /// <summary>
    /// The status text of the publishing activity.
    /// </summary>
    public required string StatusText { get; init; }

    /// <summary>
    /// Indicates whether the publishing activity is complete.
    /// </summary>
    public required bool IsComplete { get; init; }

    /// <summary>
    /// Indicates whether the publishing activity encountered an error.
    /// </summary>
    public required bool IsError { get; init; }
}

/// <summary>
/// Interface for reporting publishing activity progress.
/// </summary>
[Experimental("ASPIREPUBLISHERS001")]
public interface IPublishingActivityProgressReporter
{
    /// <summary>
    /// Creates a new publishing activity with the specified ID.
    /// </summary>
    /// <param name="id">Unique Id of the publishing activity.</param>
    /// <param name="initialStatusText"></param>
    /// <param name="isPrimary">Indicates that this activity is the primary activity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The publishing activity</returns>
    /// <remarks>
    /// When an activity is created the <paramref name="isPrimary"/> flag indicates whether this
    /// activity is the primary activity. When the primary activity is completed any laumcher
    /// which is reading activities will stop listening for updates.
    /// </remarks>
    Task<PublishingActivity> CreateActivityAsync(string id, string initialStatusText, bool isPrimary, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the status of an existing publishing activity.
    /// </summary>
    /// <param name="publishingActivity">The activity with updated properties.</param>
    /// <param name="statusUpdate"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task UpdateActivityStatusAsync(PublishingActivity publishingActivity, Func<PublishingActivityStatus, PublishingActivityStatus> statusUpdate, CancellationToken cancellationToken);
}

internal sealed class PublishingActivityProgressReporter : IPublishingActivityProgressReporter
{
    public async Task<PublishingActivity> CreateActivityAsync(string id, string initialStatusText, bool isPrimary, CancellationToken cancellationToken)
    {
        var publishingActivity = new PublishingActivity(id, isPrimary);
        await UpdateActivityStatusAsync(
            publishingActivity,
            (status) => status with
            {
                StatusText = initialStatusText,
                IsComplete = false,
                IsError = false
            },
            cancellationToken
            ).ConfigureAwait(false);

        return publishingActivity;
    }

    public async Task UpdateActivityStatusAsync(PublishingActivity publishingActivity, Func<PublishingActivityStatus, PublishingActivityStatus> statusUpdate, CancellationToken cancellationToken)
    {
        var lastStatus = publishingActivity.LastStatus ?? new PublishingActivityStatus
        {
            Activity = publishingActivity,
            StatusText = string.Empty,
            IsComplete = false,
            IsError = false
        };

        publishingActivity.LastStatus = statusUpdate(lastStatus);

        if (lastStatus == publishingActivity.LastStatus)
        {
            throw new DistributedApplicationException(
                $"The status of the publishing activity '{publishingActivity.Id}' was not updated. The status update function must return a new instance of the status."
                );
        }

        await ActivityStatusUpdated.Writer.WriteAsync(publishingActivity.LastStatus, cancellationToken).ConfigureAwait(false);

        if (publishingActivity.IsPrimary && (publishingActivity.LastStatus.IsComplete || publishingActivity.LastStatus.IsError))
        {
            // If the activity is complete or an error and it is the primary activity,
            // we can stop listening for updates.
            ActivityStatusUpdated.Writer.Complete();
        }
    }

    internal Channel<PublishingActivityStatus> ActivityStatusUpdated { get; } = Channel.CreateUnbounded<PublishingActivityStatus>();
}