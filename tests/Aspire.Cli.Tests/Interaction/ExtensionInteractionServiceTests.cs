// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Spectre.Console;
using Xunit;

namespace Aspire.Cli.Tests.Interaction;

public class ExtensionInteractionServiceTests
{
    [Fact]
    public void DisplaySuccess_TryWriteSucceeds_DoesNotLog()
    {
        // Arrange
        var fakeLogger = new FakeLogger<ExtensionInteractionService>();
        var mockBackchannel = new TestExtensionBackchannel();
        var consoleService = new ConsoleInteractionService(AnsiConsole.Console);
        
        var service = new ExtensionInteractionService(
            consoleService, 
            mockBackchannel, 
            extensionPromptEnabled: true,
            fakeLogger);

        // Act
        service.DisplaySuccess("Test message");

        // Assert - no warning should be logged since TryWrite succeeds
        Assert.DoesNotContain(fakeLogger.Collector.GetSnapshot(), log => log.Level == LogLevel.Warning);
    }

    [Fact]
    public void DisplaySuccess_WithClosedChannel_LogsWarning()
    {
        // Arrange
        var fakeLogger = new FakeLogger<ExtensionInteractionService>();
        var mockBackchannel = new TestExtensionBackchannel();
        var consoleService = new ConsoleInteractionService(AnsiConsole.Console);
        
        // Create a service and immediately close its channel to simulate failure
        var service = new ExtensionInteractionService(
            consoleService, 
            mockBackchannel, 
            extensionPromptEnabled: true,
            fakeLogger);

        // Close the channel using reflection to simulate failure scenario
        var channelField = typeof(ExtensionInteractionService)
            .GetField("_extensionTaskChannel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var channel = (Channel<Func<Task>>)channelField!.GetValue(service)!;
        channel.Writer.Complete();

        // Act
        service.DisplaySuccess("Test message");

        // Assert - warning should be logged since TryWrite fails
        var logs = fakeLogger.Collector.GetSnapshot();
        Assert.Contains(logs, log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("Failed to write to extension task channel"));
    }

    [Fact]
    public void DisplayError_CallsTryWriteDirectly_NotWrappedInAssert()
    {
        // Arrange
        var fakeLogger = new FakeLogger<ExtensionInteractionService>();
        var mockBackchannel = new TestExtensionBackchannel();
        var consoleService = new ConsoleInteractionService(AnsiConsole.Console);
        
        var service = new ExtensionInteractionService(
            consoleService, 
            mockBackchannel, 
            extensionPromptEnabled: true,
            fakeLogger);

        // Act - This should not fail in release builds
        service.DisplayError("Test error");

        // Assert - No exceptions thrown, demonstrating the fix works in release builds
        // (Previously this would have been silently skipped in release builds due to Debug.Assert)
        Assert.DoesNotContain(fakeLogger.Collector.GetSnapshot(), log => log.Level == LogLevel.Warning);
    }

    private sealed class TestExtensionBackchannel : IExtensionBackchannel
    {
        public Task ConnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken) => Task.FromResult(timestamp);
        public Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplaySuccessAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayErrorAsync(string error, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayEmptyLineAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayIncompatibleVersionErrorAsync(string requiredCapability, string appHostHostingSdkVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayCancellationMessageAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayLinesAsync(IEnumerable<DisplayLineState> lines, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisplayDashboardUrlsAsync((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ShowStatusAsync(string? status, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<T?> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken) where T : notnull => Task.FromResult<T?>(default);
        public Task<bool?> ConfirmAsync(string promptText, bool defaultValue, CancellationToken cancellationToken) => Task.FromResult<bool?>(defaultValue);
        public Task<string?> PromptForStringAsync(string promptText, string? defaultValue, Func<string, ValidationResult>? validator, bool required, CancellationToken cancellationToken) => Task.FromResult<string?>(defaultValue);
        public Task OpenProjectAsync(string projectPath, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}