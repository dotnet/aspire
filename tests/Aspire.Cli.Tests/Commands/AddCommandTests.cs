// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Tests.Commands;

public class AddCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task AddCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("add --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task AddCommandInteractiveFlowSmokeTest()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestAddCommandPrompter(interactionService);
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var dockerPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Docker",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var redisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var azureRedisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Azure.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { dockerPackage, redisPackage, azureRedisPackage } //
                        );
                };

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, nugetSource, options, cancellationToken) =>
                {
                    // Simulate adding the package.
                    return 0; // Success.
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AddCommand>();
        var result = command.Parse("add");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task AddCommandDoesNotPromptForIntegrationArgumentIfSpecifiedOnCommandLine()
    {
        var promptedForIntegrationPackages = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestAddCommandPrompter(interactionService);

                prompter.PromptForIntegrationCallback = (packages) =>
                {
                    promptedForIntegrationPackages = true;
                    throw new InvalidOperationException("Should not have been prompted for integration packages.");
                };

                return prompter;
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var dockerPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Docker",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var redisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var azureRedisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Azure.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { dockerPackage, redisPackage, azureRedisPackage } //
                        );
                };

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, nugetSource, options, cancellationToken) =>
                {
                    // Simulate adding the package.
                    return 0; // Success.
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AddCommand>();
        var result = command.Parse("add docker");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        Assert.False(promptedForIntegrationPackages);
    }

    [Fact]
    public async Task AddCommandDoesNotPromptForVersionIfSpecifiedOnCommandLine()
    {
        var promptedForIntegrationPackages = false;
        var promptedForVersion = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestAddCommandPrompter(interactionService);

                prompter.PromptForIntegrationCallback = (packages) =>
                {
                    promptedForIntegrationPackages = true;
                    throw new InvalidOperationException("Should not have been prompted for integration packages.");
                };

                prompter.PromptForIntegrationVersionCallback = (packages) =>
                {
                    promptedForVersion = true;
                    throw new InvalidOperationException("Should not have been prompted for integration version.");
                };

                return prompter;
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var dockerPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Docker",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var redisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var azureRedisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Azure.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { dockerPackage, redisPackage, azureRedisPackage } //
                        );
                };

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, nugetSource, options, cancellationToken) =>
                {
                    // Simulate adding the package.
                    return 0; // Success.
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AddCommand>();
        var result = command.Parse("add docker --version 9.2.0");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        Assert.False(promptedForIntegrationPackages);
        Assert.False(promptedForVersion);
    }

    [Fact]
    public async Task AddCommandPromptsForDisambiguation()
    {
        IEnumerable<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)>? promptedPackages = null;
        string? addedPackageName = null;
        string? addedPackageVersion = null;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestAddCommandPrompter(interactionService);

                prompter.PromptForIntegrationCallback = (packages) =>
                {
                    promptedPackages = packages;
                    return packages.Single(p => p.Package.Id == "Aspire.Hosting.Redis");
                };

                return prompter;
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var dockerPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Docker",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var redisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var azureRedisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Azure.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { dockerPackage, redisPackage, azureRedisPackage } //
                        );
                };

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, nugetSource, options, cancellationToken) =>
                {
                    addedPackageName = packageName;
                    addedPackageVersion = packageVersion;
                    return 0; // Success.
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AddCommand>();
        var result = command.Parse("add red");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        Assert.Collection(
            promptedPackages!,
            p => Assert.Equal("Aspire.Hosting.Azure.Redis", p.Package.Id),
            p => Assert.Equal("Aspire.Hosting.Redis", p.Package.Id)
            );
        Assert.Equal("Aspire.Hosting.Redis", addedPackageName);
        Assert.Equal("9.2.0", addedPackageVersion);
    }

    [Fact]
    public async Task AddCommandPreservesSourceArgumentInBothCommands()
    {
        // Arrange
        string? addUsedSource = null;
        const string expectedSource = "https://custom-nuget-source.test/v3/index.json";

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {

            // Makes it easier to isolate behavior in test case by disabling one
            // of the concurrent calls to the NuGetCache from the prefetcher.
            options.DisabledFeatures = [KnownFeatures.UpdateNotificationsEnabled];

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestAddCommandPrompter(interactionService);
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var redisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { redisPackage } //
                        );
                };

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, nugetSource, options, cancellationToken) =>
                {
                    // Capture the source used for add
                    addUsedSource = nugetSource;

                    // Simulate adding the package.
                    return 0; // Success.
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<AddCommand>();
        var result = command.Parse($"add redis --source {expectedSource}");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal(expectedSource, addUsedSource);
    }

    [Fact]
    public async Task AddCommand_EmptyPackageList_DisplaysErrorMessage()
    {
        string? displayedErrorMessage = null;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = (sp) =>
            {
                var testInteractionService = new TestConsoleInteractionService();
                testInteractionService.DisplayErrorCallback = (message) =>
                {
                    displayedErrorMessage = message;
                };
                return testInteractionService;
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    return (0, Array.Empty<NuGetPackage>());
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AddCommand>();
        var result = command.Parse("add");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToAddPackage, exitCode);
        Assert.Contains(AddCommandStrings.NoIntegrationPackagesFound, displayedErrorMessage);
    }

    [Fact]
    public async Task AddCommand_NoMatchingPackages_DisplaysNoMatchesMessage()
    {
        string? displayedSubtleMessage = null;
        bool promptedForIntegration = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = (sp) =>
            {
                var testInteractionService = new TestConsoleInteractionService();
                testInteractionService.DisplaySubtleMessageCallback = (message) =>
                {
                    displayedSubtleMessage = message;
                };
                return testInteractionService;
            };

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestAddCommandPrompter(interactionService);
                prompter.PromptForIntegrationCallback = (packages) =>
                {
                    promptedForIntegration = true;
                    return packages.First();
                };
                return prompter;
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, useCache, options, cancellationToken) =>
                {
                    var dockerPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Docker",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var redisPackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    return (0, new NuGetPackage[] { dockerPackage, redisPackage });
                };

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, nugetSource, options, cancellationToken) =>
                {
                    return 0; // Success.
                };

                return runner;
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<AddCommand>();
        var result = command.Parse("add nonexistentpackage");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
        Assert.True(promptedForIntegration);
        Assert.Equal(string.Format(AddCommandStrings.NoPackagesMatchedSearchTerm, "nonexistentpackage"), displayedSubtleMessage);
    }

    [Theory]
    [InlineData("Aspire.Hosting.Azure.Redis", "azure-redis")]
    [InlineData("CommunityToolkit.Aspire.Hosting.Cosmos", "communitytoolkit-cosmos")]
    [InlineData("Aspire.Hosting.Postgres", "postgres")]
    [InlineData("Acme.Aspire.Hosting.Foo.Bar", "acme-foo-bar")]
    [InlineData("Aspire.Hosting.Docker", "docker")]
    [InlineData("SomeOther.Package.Name", "someother-package-name")]
    public void GenerateFriendlyName_ProducesExpectedResults(string packageId, string expectedFriendlyName)
    {
        // Arrange
        var package = new NuGetPackage { Id = packageId, Version = "1.0.0", Source = "test" };

        // Act
        var result = AddCommand.GenerateFriendlyName((package, null!)); // Null is OK for this test.

        // Assert
        Assert.Equal(expectedFriendlyName, result.FriendlyName);
        Assert.Equal(package, result.Package);
    }

    [Fact]
    public async Task AddCommandPrompter_FiltersToHighestVersionPerPackageId()
    {
        // Arrange
        List<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)>? displayedPackages = null;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = (sp) =>
            {
                var mockInteraction = new TestConsoleInteractionService();
                mockInteraction.PromptForSelectionCallback = (message, choices, formatter, ct) =>
                {
                    // Capture what the prompter passes to the interaction service
                    var choicesList = choices.Cast<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)>().ToList();
                    displayedPackages = choicesList;
                    return choicesList.First();
                };
                return mockInteraction;
            };
        });
        var provider = services.BuildServiceProvider();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var prompter = new AddCommandPrompter(interactionService);

        // Create a fake channel
        var fakeCache = new FakeNuGetPackageCache();
        var channel = PackageChannel.CreateImplicitChannel(fakeCache);

        // Create multiple versions of the same package
        var packages = new[]
        {
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.0.0", Source = "nuget" }, channel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.2.0", Source = "nuget" }, channel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.1.0", Source = "nuget" }, channel),
        };

        // Act
        await prompter.PromptForIntegrationAsync(packages, CancellationToken.None);

        // Assert - should only show highest version (9.2.0) for the package ID
        Assert.NotNull(displayedPackages);
        Assert.Single(displayedPackages!);
        Assert.Equal("9.2.0", displayedPackages!.First().Package.Version);
    }

    [Fact]
    public async Task AddCommandPrompter_FiltersToHighestVersionPerChannel()
    {
        // Arrange
        List<object>? displayedChoices = null;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = (sp) =>
            {
                var mockInteraction = new TestConsoleInteractionService();
                mockInteraction.PromptForSelectionCallback = (message, choices, formatter, ct) =>
                {
                    // Capture what the prompter passes to the interaction service
                    var choicesList = choices.Cast<object>().ToList();
                    displayedChoices = choicesList;
                    return choicesList.First();
                };
                return mockInteraction;
            };
        });
        var provider = services.BuildServiceProvider();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var prompter = new AddCommandPrompter(interactionService);

        // Create a fake channel
        var fakeCache = new FakeNuGetPackageCache();
        var channel = PackageChannel.CreateImplicitChannel(fakeCache);

        // Create multiple versions of the same package from same channel
        var packages = new[]
        {
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.0.0", Source = "nuget" }, channel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.2.0", Source = "nuget" }, channel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.1.0", Source = "nuget" }, channel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.0.1-preview.1", Source = "nuget" }, channel),
        };

        // Act
        var result = await prompter.PromptForIntegrationVersionAsync(packages, CancellationToken.None);

        // Assert - For implicit channel with no explicit channels, should automatically select highest version without prompting
        Assert.Null(displayedChoices); // No prompt should be shown
        Assert.Equal("9.2.0", result.Package.Version); // Should return highest version
    }

    [Fact]
    public async Task AddCommandPrompter_ShowsHighestVersionPerChannelWhenMultipleChannels()
    {
        // Arrange
        List<object>? displayedChoices = null;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = (sp) =>
            {
                var mockInteraction = new TestConsoleInteractionService();
                mockInteraction.PromptForSelectionCallback = (message, choices, formatter, ct) =>
                {
                    // Capture what the prompter passes to the interaction service
                    var choicesList = choices.Cast<object>().ToList();
                    displayedChoices = choicesList;
                    return choicesList.First();
                };
                return mockInteraction;
            };
        });
        var provider = services.BuildServiceProvider();
        var interactionService = provider.GetRequiredService<IInteractionService>();

        var prompter = new AddCommandPrompter(interactionService);

        // Create two different channels
        var fakeCache = new FakeNuGetPackageCache();
        var implicitChannel = PackageChannel.CreateImplicitChannel(fakeCache);
        
        var mappings = new[] { new PackageMapping("Aspire*", "https://preview-feed") };
        var explicitChannel = PackageChannel.CreateExplicitChannel("preview", PackageChannelQuality.Prerelease, mappings, fakeCache);

        // Create packages from different channels with different versions
        var packages = new[]
        {
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.0.0", Source = "nuget" }, implicitChannel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.1.0", Source = "nuget" }, implicitChannel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.2.0", Source = "nuget" }, implicitChannel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "10.0.0-preview.1", Source = "preview-feed" }, explicitChannel),
            ("redis", new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "10.0.0-preview.2", Source = "preview-feed" }, explicitChannel),
        };

        // Act
        await prompter.PromptForIntegrationVersionAsync(packages, CancellationToken.None);

        // Assert - should show 2 root choices: one for implicit channel, one submenu for explicit channel
        Assert.NotNull(displayedChoices);
        Assert.Equal(2, displayedChoices!.Count);
    }

    private sealed class FakeNuGetPackageCache : Aspire.Cli.NuGet.INuGetPackageCache
    {
        public Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) 
            => Task.FromResult<IEnumerable<NuGetPackage>>([]);
        
        public Task<IEnumerable<NuGetPackage>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) 
            => Task.FromResult<IEnumerable<NuGetPackage>>([]);
        
        public Task<IEnumerable<NuGetPackage>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) 
            => Task.FromResult<IEnumerable<NuGetPackage>>([]);
        
        public Task<IEnumerable<NuGetPackage>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, bool useCache, CancellationToken cancellationToken) 
            => Task.FromResult<IEnumerable<NuGetPackage>>([]);
    }
}

internal sealed class TestAddCommandPrompter(IInteractionService interactionService) : AddCommandPrompter(interactionService)
{
    public Func<IEnumerable<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)>, (string FriendlyName, NuGetPackage Package, PackageChannel Channel)>? PromptForIntegrationCallback { get; set; }
    public Func<IEnumerable<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)>, (string FriendlyName, NuGetPackage Package, PackageChannel Channel)>? PromptForIntegrationVersionCallback { get; set; }

    public override Task<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)> PromptForIntegrationAsync(IEnumerable<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)> packages, CancellationToken cancellationToken)
    {
        return PromptForIntegrationCallback switch
        {
            { } callback => Task.FromResult(callback(packages)),
            _ => Task.FromResult(packages.First()) // If no callback is provided just accept the first package.
        };
    }

    public override Task<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)> PromptForIntegrationVersionAsync(IEnumerable<(string FriendlyName, NuGetPackage Package, PackageChannel Channel)> packages, CancellationToken cancellationToken)
    {
        return PromptForIntegrationVersionCallback switch
        {
            { } callback => Task.FromResult(callback(packages)),
            _ => Task.FromResult(packages.First()) // If no callback is provided just accept the first package.
        };
    }
}
