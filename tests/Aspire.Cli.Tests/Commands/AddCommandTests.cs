// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
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
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestAddCommandPrompter(interactionService);
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
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
    public async Task AddCommandSortsPackageVersions()
    {
        IEnumerable<(string FriendlyName, NuGetPackage Package)>? promptedPackages = null;
        
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestAddCommandPrompter(interactionService);

                prompter.PromptForIntegrationVersionCallback = (packages) =>
                {
                    promptedPackages = packages;
                    return packages.First();
                };

                return prompter;
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
                {
                    var redis92Package = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var redis93Package = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.3.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { redis92Package, redis93Package } // 
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
        Assert.Collection(
            promptedPackages!,
            p => Assert.Equal("9.3.0", p.Package.Version),
            p => Assert.Equal("9.2.0", p.Package.Version)
            );
    }

    [Fact]
    public async Task AddCommandSortsPackageVersionsWithPrerelease()
    {
        IEnumerable<(string FriendlyName, NuGetPackage Package)>? promptedPackages = null;
        
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestAddCommandPrompter(interactionService);

                prompter.PromptForIntegrationVersionCallback = (packages) =>
                {
                    promptedPackages = packages;
                    return packages.First();
                };

                return prompter;
            };

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
                {
                    var redis92Package = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.2.0"
                    };

                    var redis94PrereleasePackage = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.4.0-preview1.1234"
                    };

                    var redis93Package = new NuGetPackage()
                    {
                        Id = "Aspire.Hosting.Redis",
                        Source = "nuget",
                        Version = "9.3.0"
                    };

                    return (
                        0, // Exit code.
                        new NuGetPackage[] { redis92Package, redis94PrereleasePackage, redis93Package } // 
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
        Assert.Collection(
            promptedPackages!,
            p => Assert.Equal("9.4.0-preview1.1234", p.Package.Version),
            p => Assert.Equal("9.3.0", p.Package.Version),
            p => Assert.Equal("9.2.0", p.Package.Version)
            );
    }

    [Fact]
    public async Task AddCommandDoesNotPromptForIntegrationArgumentIfSpecifiedOnCommandLine()
    {
        var promptedForIntegrationPackages = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

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
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
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
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

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
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
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
        IEnumerable<(string FriendlyName, NuGetPackage Package)>? promptedPackages = null;
        string? addedPackageName = null;
        string? addedPackageVersion = null;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

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
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
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
        string? searchUsedSource = null;
        string? addUsedSource = null;
        const string expectedSource = "https://custom-nuget-source.test/v3/index.json";

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {

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
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
                {
                    // Capture the source used for search
                    searchUsedSource = nugetSource;

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
        Assert.Equal(expectedSource, searchUsedSource);
        Assert.Equal(expectedSource, addUsedSource);
    }

    [Fact]
    public async Task AddCommand_EmptyPackageList_DisplaysErrorMessage()
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

            options.ProjectLocatorFactory = _ => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
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
        Assert.Contains("No integration packages were found", displayedErrorMessage);
    }

    [Fact]
    public async Task AddCommand_NoMatchingPackages_DisplaysNoMatchesMessage()
    {
        string? displayedSubtleMessage = null;
        bool promptedForIntegration = false;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options => {
            options.InteractionServiceFactory = (sp) => {
                var testInteractionService = new TestConsoleInteractionService();
                testInteractionService.DisplaySubtleMessageCallback = (message) => {
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
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, options, cancellationToken) =>
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
        Assert.Contains("No packages matched your search term 'nonexistentpackage'", displayedSubtleMessage);
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
        var result = InvokeGenerateFriendlyName(package);

        // Assert
        Assert.Equal(expectedFriendlyName, result.FriendlyName);
        Assert.Equal(package, result.Package);
    }

    private static (string FriendlyName, NuGetPackage Package) InvokeGenerateFriendlyName(NuGetPackage package)
    {
        // Use reflection to access the private static method
        var method = typeof(AddCommand).GetMethod("GenerateFriendlyName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        return ((string FriendlyName, NuGetPackage Package))method!.Invoke(null, [package])!;
    }
}

internal sealed class TestAddCommandPrompter(IInteractionService interactionService) : AddCommandPrompter(interactionService)
{
    public Func<IEnumerable<(string FriendlyName, NuGetPackage Package)>, (string FriendlyName, NuGetPackage Package)>? PromptForIntegrationCallback { get; set; }
    public Func<IEnumerable<(string FriendlyName, NuGetPackage Package)>, (string FriendlyName, NuGetPackage Package)>? PromptForIntegrationVersionCallback { get; set; }

    public override Task<(string FriendlyName, NuGetPackage Package)> PromptForIntegrationAsync(IEnumerable<(string FriendlyName, NuGetPackage Package)> packages, CancellationToken cancellationToken)
    {
        return PromptForIntegrationCallback switch
        {
            { } callback => Task.FromResult(callback(packages)),
            _ => Task.FromResult(packages.First()) // If no callback is provided just accept the first package.
        };
    }

    public override Task<(string FriendlyName, NuGetPackage Package)> PromptForIntegrationVersionAsync(IEnumerable<(string FriendlyName, NuGetPackage Package)> packages, CancellationToken cancellationToken)
    {
        return PromptForIntegrationVersionCallback switch
        {
            { } callback => Task.FromResult(callback(packages)),
            _ => Task.FromResult(packages.First()) // If no callback is provided just accept the first package.
        };
    }
}
