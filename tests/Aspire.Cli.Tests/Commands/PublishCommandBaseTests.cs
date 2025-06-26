// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class PublishCommandBaseTests
{
    [Fact]
    public async Task ProcessAndDisplayPublishingActivitiesAsync_WithErrorCompletion_DisplaysFailedMessage()
    {
        // Arrange
        var activities = CreatePublishingActivities(CompletionStates.CompletedWithError, "Publishing completed with errors");

        // Act
        var result = await PublishCommandBase.ProcessAndDisplayPublishingActivitiesAsync(activities, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessAndDisplayPublishingActivitiesAsync_WithWarningCompletion_DisplaysWarningMessage()
    {
        // Arrange
        var activities = CreatePublishingActivities(CompletionStates.CompletedWithWarning, "Publishing completed with warnings");

        // Act
        var result = await PublishCommandBase.ProcessAndDisplayPublishingActivitiesAsync(activities, CancellationToken.None);

        // Assert
        Assert.True(result); // Should return true for warnings (not considered failures)
    }

    [Fact]
    public async Task ProcessAndDisplayPublishingActivitiesAsync_WithSuccessCompletion_DisplaysCompletedMessage()
    {
        // Arrange
        var activities = CreatePublishingActivities(CompletionStates.Completed, "Publishing completed successfully");

        // Act
        var result = await PublishCommandBase.ProcessAndDisplayPublishingActivitiesAsync(activities, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    private static async IAsyncEnumerable<PublishingActivity> CreatePublishingActivities(string completionState, string statusText)
    {
        await Task.Yield(); // Make this truly async
        yield return new PublishingActivity
        {
            Type = PublishingActivityTypes.PublishComplete,
            Data = new PublishingActivityData
            {
                Id = PublishingActivityTypes.PublishComplete,
                StatusText = statusText,
                CompletionState = completionState
            }
        };
    }
}