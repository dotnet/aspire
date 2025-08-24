// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Tests.DotNet;

public class DotNetRuntimeSelectorCachingTests
{
    [Fact]
    public async Task InitializeAsync_WhenUserDeclinesInstallation_DoesNotPromptAgain()
    {
        // Arrange
        var logger = NullLogger<DotNetRuntimeSelector>.Instance;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_DISABLE_PRIVATE_SDK"] = null,
                ["ASPIRE_AUTO_INSTALL"] = null // Force user prompt
            })
            .Build();

        var sdkInstaller = new TestSdkInstaller { CheckResult = false }; // System SDK not available
        var interactionService = new TestInteractionServiceWithConfirmTracking(false); // User declines
        var console = new TestAnsiConsole();

        var selector = new DotNetRuntimeSelector(logger, configuration, sdkInstaller, interactionService, console);

        // Act - First call should prompt the user
        var firstResult = await selector.InitializeAsync();
        
        // Act - Second call should use cached result and not prompt again
        var secondResult = await selector.InitializeAsync();

        // Assert
        Assert.False(firstResult);
        Assert.False(secondResult);
        
        // Verify user was only prompted once
        Assert.Equal(1, interactionService.ConfirmCallCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenSystemSDKAvailable_CachesResult()
    {
        // Arrange
        var logger = NullLogger<DotNetRuntimeSelector>.Instance;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var sdkInstaller = new TestSdkInstaller { CheckResult = true }; // System SDK is available
        var interactionService = new TestInteractionServiceWithConfirmTracking(true);
        var console = new TestAnsiConsole();

        var selector = new DotNetRuntimeSelector(logger, configuration, sdkInstaller, interactionService, console);

        // Act
        var firstResult = await selector.InitializeAsync();
        var secondResult = await selector.InitializeAsync();

        // Assert
        Assert.True(firstResult);
        Assert.True(secondResult);
        Assert.Equal("dotnet", selector.DotNetExecutablePath);
        Assert.Equal(DotNetRuntimeMode.System, selector.Mode);
        
        // Should not have prompted user at all since system SDK was available
        Assert.Equal(0, interactionService.ConfirmCallCount);
    }

    private sealed class TestSdkInstaller : IDotNetSdkInstaller
    {
        public bool CheckResult { get; set; }
        public string? LastCheckedVersion { get; set; }

        public Task<bool> CheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult);
        }

        public Task<bool> CheckAsync(string minimumVersion, CancellationToken cancellationToken = default)
        {
            LastCheckedVersion = minimumVersion;
            return Task.FromResult(CheckResult);
        }

        public Task InstallAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestInteractionServiceWithConfirmTracking : IInteractionService
    {
        private readonly bool _confirmResponse;

        public TestInteractionServiceWithConfirmTracking(bool confirmResponse)
        {
            _confirmResponse = confirmResponse;
        }

        public int ConfirmCallCount { get; private set; }

        public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action) => action();
        public void ShowStatus(string statusText, Action action) => action();
        public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, Spectre.Console.ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default) => Task.FromResult(defaultValue ?? string.Empty);
        
        public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
        {
            ConfirmCallCount++;
            return Task.FromResult(_confirmResponse);
        }
        
        public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull => Task.FromResult(choices.First());
        public int DisplayIncompatibleVersionError(Aspire.Cli.Backchannel.AppHostIncompatibleException ex, string appHostHostingVersion) => 1;
        public void DisplayError(string errorMessage) { }
        public void DisplayMessage(string emoji, string message) { }
        public void DisplayPlainText(string text) { }
        public void DisplayMarkdown(string markdown) { }
        public void DisplaySuccess(string message) { }
        public void DisplaySubtleMessage(string message) { }
        public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls) { }
        public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) { }
        public void DisplayCancellationMessage() { }
        public void DisplayEmptyLine() { }
        public void DisplayVersionUpdateNotification(string newerVersion) { }
        public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false) { }
    }

    private sealed class TestAnsiConsole : IAnsiConsole
    {
        public Profile Profile => new Profile(new TestConsoleOutput(), Encoding.UTF8);
        public IAnsiConsoleCursor Cursor => throw new NotImplementedException();
        public IAnsiConsoleInput Input => throw new NotImplementedException();
        public IExclusivityMode ExclusivityMode => throw new NotImplementedException();
        public RenderPipeline Pipeline => throw new NotImplementedException();

        public void Clear(bool home) { }
        public void Write(IRenderable renderable) { }
    }

    private sealed class TestConsoleOutput : IAnsiConsoleOutput
    {
        public TextWriter Writer => Console.Out;
        public bool IsTerminal => false;
        public int Width => 80;
        public int Height => 25;

        public void SetEncoding(Encoding encoding) { }
    }
}