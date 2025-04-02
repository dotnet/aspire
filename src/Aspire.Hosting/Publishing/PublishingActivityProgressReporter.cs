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
    internal PublishingActivity(string id, string initialStatusText, bool isPrimary = false)
    {
        Id = id;
        StatusMessage = initialStatusText;
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// Unique Id of the publishing activity.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// Status message of the publishing activity.
    /// </summary>
    public string StatusMessage { get; set; }

    /// <summary>
    /// Indicates whether the publishing activity is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Indicates whether the publishing activity is the primary activity.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Indicates whether the publishing activity has encountered an error.
    /// </summary>
    public bool IsError { get; set; }

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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task UpdateActivityAsync(PublishingActivity publishingActivity, CancellationToken cancellationToken);
}

internal sealed class PublishingActivityProgressReporter : IPublishingActivityProgressReporter
{
    public async Task<PublishingActivity> CreateActivityAsync(string id, string initialStatusText, bool isPrimary, CancellationToken cancellationToken)
    {
        var publishingActivity = new PublishingActivity(id, initialStatusText, isPrimary);
        await ActivitiyUpdated.Writer.WriteAsync(publishingActivity, cancellationToken).ConfigureAwait(false);
        return publishingActivity;
    }

    public async Task UpdateActivityAsync(PublishingActivity publishingActivity, CancellationToken cancellationToken)
    {
        await ActivitiyUpdated.Writer.WriteAsync(publishingActivity, cancellationToken).ConfigureAwait(false);

        if (publishingActivity.IsPrimary && (publishingActivity.IsComplete || publishingActivity.IsError))
        {
            // If the activity is complete or an error and it is the primary activity,
            // we can stop listening for updates.
            ActivitiyUpdated.Writer.Complete();
        }
    }

    internal Channel<PublishingActivity> ActivitiyUpdated { get; } = Channel.CreateUnbounded<PublishingActivity>();
}