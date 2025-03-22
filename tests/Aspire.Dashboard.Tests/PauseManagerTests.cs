// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ConsoleLogs;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class PauseManagerTests
{
    [Fact]
    public void SetConsoleLogsPaused_ShouldAddPauseRange_WhenPaused()
    {
        // Arrange
        var pauseManager = new PauseManager();
        var timestamp = DateTime.UtcNow;

        // Act
        pauseManager.SetConsoleLogsPaused(true, timestamp);

        // Assert
        Assert.True(pauseManager.ConsoleLogsPaused);
        Assert.Single(pauseManager.ConsoleLogsPausedRanges);
        Assert.Equal(timestamp, pauseManager.ConsoleLogsPausedRanges.Keys.First());
    }

    [Fact]
    public void SetConsoleLogsPaused_ShouldUpdatePauseRange_WhenResumed()
    {
        // Arrange
        var pauseManager = new PauseManager();
        var startTimestamp = DateTime.UtcNow;
        pauseManager.SetConsoleLogsPaused(true, startTimestamp);
        var endTimestamp = startTimestamp.AddMinutes(5);

        // Act
        pauseManager.SetConsoleLogsPaused(false, endTimestamp);

        // Assert
        Assert.False(pauseManager.ConsoleLogsPaused);
        Assert.Single(pauseManager.ConsoleLogsPausedRanges);
        var pauseRange = pauseManager.ConsoleLogsPausedRanges.Values.First();
        Assert.Equal(startTimestamp, pauseRange.Start);
        Assert.Equal(endTimestamp, pauseRange.End);
    }

    [Theory]
    [InlineData(true, 1, true, 1)]
    [InlineData(false, 6, false, 0)]
    public void IsConsoleLogFiltered_ShouldReturnExpectedResult(bool isPaused, int minutesToAdd, bool expectedResult, int expectedFilteredLogCount)
    {
        // Arrange
        var pauseManager = new PauseManager();
        var startTimestamp = DateTime.UtcNow;
        pauseManager.SetConsoleLogsPaused(true, startTimestamp);
        if (!isPaused)
        {
            var endTimestamp = startTimestamp.AddMinutes(5);
            pauseManager.SetConsoleLogsPaused(false, endTimestamp);
        }
        var logTimestamp = startTimestamp.AddMinutes(minutesToAdd);
        var entry = LogEntry.Create(logTimestamp, "msg", false);

        // Act
        var isFiltered1 = pauseManager.IsConsoleLogFiltered(entry, "app1");
        var isFiltered2 = pauseManager.IsConsoleLogFiltered(entry, "app1");
        var isFiltered3 = pauseManager.IsConsoleLogFiltered(entry, "app1");

        // Assert
        Assert.Equal(expectedResult, isFiltered1);
        Assert.Equal(expectedResult, isFiltered2);
        Assert.Equal(expectedResult, isFiltered3);

        if (pauseManager.TryGetConsoleLogPause(startTimestamp, out var pause))
        {
            Assert.Equal(expectedFilteredLogCount, pause.GetFilteredLogCount("app1"));
        }
    }

    [Fact]
    public void TryGetConsoleLogPause_ShouldReturnTrue_WhenPauseExists()
    {
        // Arrange
        var pauseManager = new PauseManager();
        var startTimestamp = DateTime.UtcNow;
        pauseManager.SetConsoleLogsPaused(true, startTimestamp);

        // Act
        var result = pauseManager.TryGetConsoleLogPause(startTimestamp, out var pause);

        // Assert
        Assert.True(result);
        Assert.NotNull(pause);
        Assert.Equal(startTimestamp, pause.Start);
    }

    [Fact]
    public void TryGetConsoleLogPause_ShouldReturnFalse_WhenPauseDoesNotExist()
    {
        // Arrange
        var pauseManager = new PauseManager();
        var startTimestamp = DateTime.UtcNow;

        // Act
        var result = pauseManager.TryGetConsoleLogPause(startTimestamp, out var pause);

        // Assert
        Assert.False(result);
        Assert.Null(pause);
    }
}
