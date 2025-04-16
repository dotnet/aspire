// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class AddCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task AddCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("add --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task AddCommandInteractiveFlowSmokeTest()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options => {

            options.AddCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                return new TestAddCommandPrompter(interactionService);
            };

            options.ProjectLocatorFactory = _ => new FakeProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, cancellationToken) =>
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

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, cancellationToken) =>
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

        var services = CliTestHelper.CreateServiceCollection(outputHelper, options => {

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

            options.ProjectLocatorFactory = _ => new FakeProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, cancellationToken) =>
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

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, cancellationToken) =>
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

        var services = CliTestHelper.CreateServiceCollection(outputHelper, options => {

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

            options.ProjectLocatorFactory = _ => new FakeProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, cancellationToken) =>
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

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, cancellationToken) =>
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

        var services = CliTestHelper.CreateServiceCollection(outputHelper, options => {

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

            options.ProjectLocatorFactory = _ => new FakeProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (dir, query, prerelease, take, skip, nugetSource, cancellationToken) =>
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

                runner.AddPackageAsyncCallback = (projectFilePath, packageName, packageVersion, cancellationToken) =>
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
            p => Assert.Equal("Aspire.Hosting.Redis", p.Package.Id),
            p => Assert.Equal("Aspire.Hosting.Azure.Redis", p.Package.Id)
            );
        Assert.Equal("Aspire.Hosting.Redis", addedPackageName);
        Assert.Equal("9.2.0", addedPackageVersion);
    }

}

internal sealed class FakeProjectLocator : IProjectLocator
{
    public FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
    {
        if (projectFile != null)
        {
            return projectFile;
        }

        var fakeProjectFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "AppHost.csproj");
        return new FileInfo(fakeProjectFilePath);
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
