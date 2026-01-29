// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Cli.Interaction;
using Aspire.Cli.Backchannel;
using Spectre.Console;

namespace Aspire.Cli.Tests.Commands;

public class CacheCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task CacheCommand_WithExtensionMode_Works()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper,
            options =>
            {
                options.ConfigurationCallback += config =>
                {
                    // Enable extension mode for testing
                    config["ASPIRE_EXTENSION_PROMPT_ENABLED"] = "true";
                    config["ASPIRE_EXTENSION_TOKEN"] = "token";
                };

                options.InteractionServiceFactory = sp => new TestExtensionInteractionService(sp);
                options.ExtensionBackchannelFactory = sp => new TestExtensionBackchannel();
            });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("cache");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task CacheCommand_WithoutExtensionMode_ReturnsInvalidCommandExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("cache");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task CacheClearCommand_WithExtensionMode_PromptsForConfirmation()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Custom interaction service that returns "true" for confirmation
        var testInteractionService = new TestCacheInteractionService(true);
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper,
            options =>
            {
                options.ConfigurationCallback += config =>
                {
                    // Enable extension mode for testing
                    config["ASPIRE_EXTENSION_PROMPT_ENABLED"] = "true";
                    config["ASPIRE_EXTENSION_TOKEN"] = "token";
                };

                options.InteractionServiceFactory = sp => testInteractionService;
                options.ExtensionBackchannelFactory = sp => new TestExtensionBackchannel();
            });
        var provider = services.BuildServiceProvider();
        
        // Create some cache files to verify they get deleted
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var cacheDir = executionContext.CacheDirectory;
        cacheDir.Create();
        var testFile = Path.Combine(cacheDir.FullName, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("cache clear");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        
        // Verify the cache file was deleted
        Assert.False(File.Exists(testFile));
    }

    [Fact]
    public async Task CacheClearCommand_WithExtensionMode_CancelsWhenNotConfirmed()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Custom interaction service that returns "false" for confirmation
        var testInteractionService = new TestCacheInteractionService(false);
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper,
            options =>
            {
                options.ConfigurationCallback += config =>
                {
                    // Enable extension mode for testing
                    config["ASPIRE_EXTENSION_PROMPT_ENABLED"] = "true";
                    config["ASPIRE_EXTENSION_TOKEN"] = "token";
                };

                options.InteractionServiceFactory = sp => testInteractionService;
                options.ExtensionBackchannelFactory = sp => new TestExtensionBackchannel();
            });
        var provider = services.BuildServiceProvider();
        
        // Create some cache files to verify they don't get deleted
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var cacheDir = executionContext.CacheDirectory;
        cacheDir.Create();
        var testFile = Path.Combine(cacheDir.FullName, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("cache clear");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        
        // Verify the cache file was NOT deleted
        Assert.True(File.Exists(testFile));
    }

    [Fact]
    public async Task CacheClearCommand_WithoutExtensionMode_ClearsCache()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        
        // Create some cache files to verify they get deleted
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var cacheDir = executionContext.CacheDirectory;
        cacheDir.Create();
        var testFile = Path.Combine(cacheDir.FullName, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("cache clear");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        
        // Verify the cache file was deleted
        Assert.False(File.Exists(testFile));
    }

    [Fact]
    public async Task CacheClearCommand_WithEmptyCache_ReportsAlreadyEmpty()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<Aspire.Cli.Commands.RootCommand>();
        var result = command.Parse("cache clear");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    // Test helper class for testing cache command confirmation
    private sealed class TestCacheInteractionService : IInteractionService
    {
        private readonly bool _confirmationValue;

        public TestCacheInteractionService(bool confirmationValue)
        {
            _confirmationValue = confirmationValue;
        }

        public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action) => action();
        public void ShowStatus(string statusText, Action action) => action();
        public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default) => Task.FromResult(_confirmationValue);
        
        public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
        {
            // Return the confirmation value when selecting from bool choices
            if (typeof(T) == typeof(bool))
            {
                return Task.FromResult((T)(object)_confirmationValue);
            }
            // For other types (like subcommands), return the first choice
            return Task.FromResult(choices.First());
        }
        
        public Task<IReadOnlyList<T>> PromptForSelectionsAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull => Task.FromResult<IReadOnlyList<T>>(choices.ToList());
        public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion) => 0;
        public void DisplayError(string errorMessage) { }
        public void DisplayMessage(string emoji, string message) { }
        public void DisplayPlainText(string text) { }
        public void DisplayRawText(string text) { }
        public void DisplayMarkdown(string markdown) { }
        public void DisplayMarkupLine(string markup) { }
        public void DisplaySuccess(string message) { }
        public void DisplaySubtleMessage(string message, bool escapeMarkup = true) { }
        public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) { }
        public void DisplayCancellationMessage() { }
        public void DisplayEmptyLine() { }
        public void DisplayVersionUpdateNotification(string newerVersion, string? updateCommand = null) { }
        public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false) { }
    }
}
