// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Templating;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Tests.Commands;

public class NewCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task NewCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("new --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11099")]
    public async Task NewCommandInteractiveFlowSmokeTest()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestNewCommandPrompter(interactionService);
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new aspire-starter --use-redis-cache --test-framework None");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/10987")]
    // Quarantined due to flakiness. See linked issue for details.
    public async Task NewCommandDerivesOutputPathFromProjectNameForStarterTemplate()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestNewCommandPrompter(interactionService);

                prompter.PromptForProjectNameCallback = (defaultName) =>
                {
                    return "CustomName";
                };

                prompter.PromptForOutputPathCallback = (path) =>
                {
                    Assert.Equal("./CustomName", path);
                    return path;
                };

                return prompter;
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("new aspire-starter --use-redis-cache --test-framework None");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11034")]
    public async Task NewCommandDoesNotPromptForProjectNameIfSpecifiedOnCommandLine()
    {
        var promptedForName = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestNewCommandPrompter(interactionService);

                prompter.PromptForProjectNameCallback = (defaultName) =>
                {
                    promptedForName = true;
                    throw new InvalidOperationException("This should not be called");
                };

                return prompter;
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new aspire-starter --name MyApp --output . --use-redis-cache --test-framework None");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        Assert.False(promptedForName);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11222")]
    public async Task NewCommandDoesNotPromptForOutputPathIfSpecifiedOnCommandLine()
    {
        bool promptedForPath = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestNewCommandPrompter(interactionService);

                prompter.PromptForOutputPathCallback = (path) =>
                {
                    promptedForPath = true;
                    throw new InvalidOperationException("This should not be called");
                };

                return prompter;
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new aspire-starter --output notsrc --use-redis-cache --test-framework None");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        Assert.False(promptedForPath);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/10979")]
    // Quarantined due to flakiness. See linked issue for details.
    public async Task NewCommandDoesNotPromptForTemplateIfSpecifiedOnCommandLine()
    {
        bool promptedForTemplate = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestNewCommandPrompter(interactionService);

                prompter.PromptForTemplateCallback = (path) =>
                {
                    promptedForTemplate = true;
                    throw new InvalidOperationException("This should not be called");
                };

                return prompter;
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new aspire-starter --name MyApp --output . --use-redis-cache --test-framework None");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        Assert.False(promptedForTemplate);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11172")]
    public async Task NewCommandDoesNotPromptForTemplateVersionIfSpecifiedOnCommandLine()
    {
        bool promptedForTemplateVersion = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestNewCommandPrompter(interactionService);

                prompter.PromptForTemplatesVersionCallback = (packages) =>
                {
                    promptedForTemplateVersion = true;
                    throw new InvalidOperationException("This should not be called");
                };

                return prompter;
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetConfigFile, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new aspire-starter --name MyApp --output . --use-redis-cache --test-framework None --version 9.2.0");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.LongTimeout);
        Assert.Equal(0, exitCode);
        Assert.False(promptedForTemplateVersion);
    }

    [Fact]
    public async Task NewCommand_EmptyPackageList_DisplaysErrorMessage()
    {
        string? displayedErrorMessage = null;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {
            options.InteractionServiceFactory = (sp) => {
                var testInteractionService = new TestConsoleInteractionService();
                testInteractionService.DisplayErrorCallback = (message) => {
                    displayedErrorMessage = message;
                };
                return testInteractionService;
            };

            options.DotNetCliRunnerFactory = (sp) => {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) => {
                    return (0, Array.Empty<NuGetPackage>());
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        Assert.Equal(ExitCodeConstants.FailedToCreateNewProject, exitCode);
        Assert.Contains(TemplatingStrings.NoTemplateVersionsFound, displayedErrorMessage);
    }

    [Fact]
    public async Task NewCommand_WhenCertificateServiceThrows_ReturnsNonZeroExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.NewCommandPrompterFactory = (sp) => {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestNewCommandPrompter(interactionService);
                return prompter;
            };
            options.CertificateServiceFactory = _ => new ThrowingCertificateService();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                runner.InstallTemplateAsyncCallback = (packageName, version, nugetSource, force, options, cancellationToken) =>
                {
                    return (0, version); // Success, return the template version
                };

                runner.NewProjectAsyncCallback = (templateName, name, outputPath, options, cancellationToken) =>
                {
                    return 0; // Success
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("new aspire-starter --use-redis-cache --test-framework None");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToTrustCertificates, exitCode);
    }

    [Fact]
    public async Task NewCommandWithExitCode73ShowsUserFriendlyError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            // Set of options that we'll give when prompted.
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestNewCommandPrompter(interactionService);
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                runner.InstallTemplateAsyncCallback = (packageName, version, nugetSource, force, options, cancellationToken) =>
                {
                    return (0, version); // Success, return the template version
                };

                runner.NewProjectAsyncCallback = (templateName, name, outputPath, options, cancellationToken) =>
                {
                    return 73; // Simulate exit code 73 (directory already contains files)
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("new aspire-starter --use-redis-cache --test-framework None");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToCreateNewProject, exitCode);
    }

    private sealed class ThrowingCertificateService : ICertificateService
    {
        public Task EnsureCertificatesTrustedAsync(IDotNetCliRunner runner, CancellationToken cancellationToken)
        {
            throw new CertificateServiceException("Failed to trust certificates");
        }
    }

    [Fact]
    public async Task NewCommandPromptsForTemplateVersionBeforeTemplateOptions()
    {
        var operationOrder = new List<string>();

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.NewCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestNewCommandPrompter(interactionService);

                prompter.PromptForTemplatesVersionCallback = (packages) =>
                {
                    operationOrder.Add("TemplateVersion");
                    return packages.First();
                };

                return prompter;
            };

            options.InteractionServiceFactory = (sp) =>
            {
                var testInteractionService = new OrderTrackingInteractionService(operationOrder);
                return testInteractionService;
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetConfigFile, useCache, options, cancellationToken) =>
                {
                    var package = new NuGetPackage()
                    {
                        Id = "Aspire.ProjectTemplates",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { package } // Single package.
                        );
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new aspire-starter --name TestApp --output .");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);

        // Verify that template version was prompted before template options
        Assert.Contains("TemplateVersion", operationOrder);

        // If template options were prompted, they should come after version selection
        var versionIndex = operationOrder.IndexOf("TemplateVersion");
        var optionIndex = operationOrder.IndexOf("TemplateOption");

        if (optionIndex >= 0)
        {
            Assert.True(versionIndex < optionIndex,
                $"Template version should be prompted before template options. Order: {string.Join(", ", operationOrder)}");
        }
    }
}

internal sealed class TestNewCommandPrompter(IInteractionService interactionService) : NewCommandPrompter(interactionService)
{
    public Func<IEnumerable<(NuGetPackage Package, PackageChannel Channel)>, (NuGetPackage Package, PackageChannel Channel)>? PromptForTemplatesVersionCallback { get; set; }
    public Func<ITemplate[], ITemplate>? PromptForTemplateCallback { get; set; }
    public Func<string, string>? PromptForProjectNameCallback { get; set; }
    public Func<string, string>? PromptForOutputPathCallback { get; set; }

    public override Task<ITemplate> PromptForTemplateAsync(ITemplate[] validTemplates, CancellationToken cancellationToken)
    {
        return PromptForTemplateCallback switch
        {
            { } callback => Task.FromResult(callback(validTemplates)),
            _ => Task.FromResult(validTemplates[0]) // If no callback is provided just accept the first template.
        };
    }

    public override Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken)
    {
        return PromptForProjectNameCallback switch
        {
            { } callback => Task.FromResult(callback(defaultName)),
            _ => Task.FromResult(defaultName) // If no callback is provided just accept the default.
        };
    }

    public override Task<string> PromptForOutputPath(string path, CancellationToken cancellationToken)
    {
        return PromptForOutputPathCallback switch
        {
            { } callback => Task.FromResult(callback(path)),
            _ => Task.FromResult(path) // If no callback is provided just accept the default.
        };
    }

    public override Task<(NuGetPackage Package, PackageChannel Channel)> PromptForTemplatesVersionAsync(IEnumerable<(NuGetPackage Package, PackageChannel Channel)> candidatePackages, CancellationToken cancellationToken)
    {
        return PromptForTemplatesVersionCallback switch
        {
            { } callback => Task.FromResult(callback(candidatePackages)),
            _ => Task.FromResult(candidatePackages.First()) // If no callback is provided just accept the first package.
        };
    }
}

internal sealed class OrderTrackingInteractionService(List<string> operationOrder) : IInteractionService
{
    public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        return action();
    }

    public void ShowStatus(string statusText, Action action)
    {
        action();
    }

    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(defaultValue ?? string.Empty);
    }

    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        if (!choices.Any())
        {
            throw new EmptyChoicesException($"No items available for selection: {promptText}");
        }

        // Track template option prompts
        if (promptText?.Contains("Redis") == true ||
            promptText?.Contains("test framework") == true ||
            promptText?.Contains("Create a test project") == true ||
            promptText?.Contains("xUnit") == true)
        {
            operationOrder.Add("TemplateOption");
        }

        return Task.FromResult(choices.First());
    }

    public Task<IReadOnlyList<T>> PromptForSelectionsAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        if (!choices.Any())
        {
            throw new EmptyChoicesException($"No items available for selection: {promptText}");
        }

        return Task.FromResult<IReadOnlyList<T>>(choices.ToList());
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion) => 0;
    public void DisplayError(string errorMessage) { }
    public void DisplayMessage(string emoji, string message) { }
    public void DisplaySuccess(string message) { }
    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) { }
    public void DisplayCancellationMessage() { }
    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public void DisplaySubtleMessage(string message, bool escapeMarkup = true) { }
    public void DisplayEmptyLine() { }
    public void DisplayPlainText(string text) { }
    public void DisplayMarkdown(string markdown) { }
    public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false) { }
    public void DisplayVersionUpdateNotification(string newerVersion, string? updateCommand = null) { }
}
