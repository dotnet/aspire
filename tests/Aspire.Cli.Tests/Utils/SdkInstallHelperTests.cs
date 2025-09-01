// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class SdkInstallHelperTests
{
    [Fact]
    public async Task EnsureSdkInstalledAsync_WithRuntimeSelector_CallsInitializeAsync()
    {
        // Arrange
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => true
        };
        var interactionService = new TestInteractionService();
        var runtimeSelector = new TestRuntimeSelector { InitializeResult = true };

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(sdkInstaller, interactionService, runtimeSelector);

        // Assert
        Assert.True(result);
        Assert.True(runtimeSelector.InitializeCalled);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WithRuntimeSelector_WhenInitializeFails_ReturnsFalse()
    {
        // Arrange
        var sdkInstaller = new TestDotNetSdkInstaller();
        var interactionService = new TestInteractionService();
        var runtimeSelector = new TestRuntimeSelector { InitializeResult = false };

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(sdkInstaller, interactionService, runtimeSelector);

        // Assert
        Assert.False(result);
        Assert.True(runtimeSelector.InitializeCalled);
        Assert.True(interactionService.ErrorDisplayed);
    }

    private sealed class TestRuntimeSelector : IDotNetRuntimeSelector
    {
        public bool InitializeResult { get; set; } = true;
        public bool InitializeCalled { get; private set; }

        public string GetDotNetExecutablePath() => "dotnet";
        public DotNetRuntimeMode Mode => DotNetRuntimeMode.System;

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            InitializeCalled = true;
            return await Task.FromResult(InitializeResult);
        }

        public IDictionary<string, string> GetEnvironmentVariables()
        {
            return new Dictionary<string, string>();
        }
    }

    private sealed class TestInteractionService : IInteractionService
    {
        public bool ErrorDisplayed { get; private set; }

        public void DisplayError(string errorMessage) => ErrorDisplayed = true;

        // Minimal implementations for required interface methods
        public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action) => action();
        public void ShowStatus(string statusText, Action action) => action();
        public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, Spectre.Console.ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default) => Task.FromResult(defaultValue ?? string.Empty);
        public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default) => Task.FromResult(defaultValue);
        public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull => Task.FromResult(choices.First());
        public int DisplayIncompatibleVersionError(Aspire.Cli.Backchannel.AppHostIncompatibleException ex, string appHostHostingVersion) => 1;
        public void DisplayMessage(string emoji, string message) { }
        public void DisplayPlainText(string text) { }
        public void DisplayMarkdown(string markdown) { }
        public void DisplaySuccess(string message) { }
        public void DisplaySubtleMessage(string message) { }
        
#pragma warning disable CA1822 // Mark members as static
        public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls) 
        { 
            _ = dashboardUrls; 
            // Intentionally empty - this is a test stub
        }
#pragma warning restore CA1822 // Mark members as static
        
        public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) { }
        public void DisplayCancellationMessage() { }
        public void DisplayEmptyLine() { }
        public void DisplayVersionUpdateNotification(string newerVersion) { }
        public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false) { }
    }
}