// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class UpdateCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task UpdateCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WithNoAspirePackages_ReturnsSuccess()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = _ =>
            {
                var runner = new TestDotNetCliRunner();
                runner.GetProjectItemsAndPropertiesAsyncCallback = (projectFile, items, properties, options, cancellationToken) =>
                {
                    // Return empty JSON with no PackageReference items
                    var emptyJson = """
                    {
                        "Properties": {
                            "AspireHostingSDKVersion": "9.0.0"
                        },
                        "Items": {
                            "PackageReference": []
                        }
                    }
                    """;
                    return (0, System.Text.Json.JsonDocument.Parse(emptyJson));
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WithAspirePackagesButNoUpdates_ReturnsSuccess()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = _ =>
            {
                var runner = new TestDotNetCliRunner();
                runner.GetProjectItemsAndPropertiesAsyncCallback = (projectFile, items, properties, options, cancellationToken) =>
                {
                    // Return JSON with Aspire packages - the stub logic will simulate no updates
                    var jsonWithAspirePackages = """
                    {
                        "Properties": {
                            "AspireHostingSDKVersion": "9.0.0"
                        },
                        "Items": {
                            "PackageReference": [
                                {
                                    "Identity": "Aspire.Hosting.Redis",
                                    "Version": "9.0.0"
                                }
                            ]
                        }
                    }
                    """;
                    return (0, System.Text.Json.JsonDocument.Parse(jsonWithAspirePackages));
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_ProjectFileNotFound_ReturnsFailure()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new ProjectFileDoesNotExistLocator();
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    private sealed class ProjectFileDoesNotExistLocator : Aspire.Cli.Projects.IProjectLocator
    {
        public Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken)
        {
            throw new Aspire.Cli.Projects.ProjectLocatorException("Project file does not exist.");
        }
    }

    [Fact]
    public async Task UpdateCommand_WithAspirePackagesAndUpdatesAvailable_ShowsUpdatePrompt()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = _ =>
            {
                var runner = new TestDotNetCliRunner();
                runner.GetProjectItemsAndPropertiesAsyncCallback = (projectFile, items, properties, options, cancellationToken) =>
                {
                    // Return JSON with Aspire packages that have available updates
                    var jsonWithAspirePackages = """
                    {
                        "Properties": {
                            "AspireHostingSDKVersion": "9.0.0"
                        },
                        "Items": {
                            "PackageReference": [
                                {
                                    "Identity": "Aspire.Hosting.Redis",
                                    "Version": "9.0.0"
                                },
                                {
                                    "Identity": "CommunityToolkit.Aspire.Hosting.SqlDatabaseProjects",
                                    "Version": "1.0.0"
                                }
                            ]
                        }
                    }
                    """;
                    return (0, System.Text.Json.JsonDocument.Parse(jsonWithAspirePackages));
                };

                return runner;
            };

            // Mock interaction service to auto-cancel confirmation
            options.InteractionServiceFactory = _ => new TestInteractionService(confirmResult: false);
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode); // Should succeed even when cancelled
    }

    private sealed class TestInteractionService(bool confirmResult) : Aspire.Cli.Interaction.IInteractionService
    {
        public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
        {
            return action();
        }

        public void ShowStatus(string statusText, Action action)
        {
            action();
        }

        public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, Spectre.Console.ValidationResult>? validator = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(defaultValue ?? "test");
        }

        public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(confirmResult);
        }

        public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
        {
            return Task.FromResult(choices.First());
        }

        public int DisplayIncompatibleVersionError(Aspire.Cli.Backchannel.AppHostIncompatibleException ex, string appHostHostingVersion)
        {
            return 1;
        }

        public void DisplayError(string errorMessage) { }
        public void DisplayMessage(string emoji, string message) { }
        public void DisplaySuccess(string message) { }
        public void DisplaySubtleMessage(string message) { }
        public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls) { }
        public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) { }
        public void DisplayCancellationMessage() { }
        public void DisplayEmptyLine() { }
    }
}