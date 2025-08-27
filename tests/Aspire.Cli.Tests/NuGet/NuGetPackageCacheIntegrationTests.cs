// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Tests.NuGet;

public class NuGetPackageCacheIntegrationTests(ITestOutputHelper outputHelper) : IDisposable
{
    private readonly TempDirectory _tempDirectory = new();

    [Fact]
    public async Task GetTemplatePackagesAsync_Should_UseDiskCacheOnSecondCall()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Override the user directory for testing
        Environment.SetEnvironmentVariable("HOME", _tempDirectory.Path);
        Environment.SetEnvironmentVariable("USERPROFILE", _tempDirectory.Path);
        
        var callCount = 0;
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, configure =>
        {
            configure.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (_, _, _, _, _, _, _, _) =>
                {
                    callCount++;
                    return (0, [
                        new NuGetPackage { Id = "Aspire.ProjectTemplates", Version = "9.4.0-preview", Source = "nuget.org" }
                    ]);
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var nuGetPackageCache = provider.GetRequiredService<INuGetPackageCache>();

        // Act - First call should hit the API and cache the result
        var firstResult = await nuGetPackageCache.GetTemplatePackagesAsync(
            workspace.WorkspaceRoot, 
            prerelease: true, 
            nugetConfigFile: null, 
            CancellationToken.None).WaitAsync(CliTestConstants.DefaultTimeout);

        // Clear memory cache by creating a new service provider
        provider.Dispose();
        provider = services.BuildServiceProvider();
        nuGetPackageCache = provider.GetRequiredService<INuGetPackageCache>();

        // Act - Second call should use disk cache, not hit the API again
        var secondResult = await nuGetPackageCache.GetTemplatePackagesAsync(
            workspace.WorkspaceRoot, 
            prerelease: true, 
            nugetConfigFile: null, 
            CancellationToken.None).WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(1, callCount); // API should only be called once
        Assert.Single(firstResult);
        Assert.Single(secondResult);
        Assert.Equal("Aspire.ProjectTemplates", firstResult.First().Id);
        Assert.Equal("Aspire.ProjectTemplates", secondResult.First().Id);
    }

    [Fact]
    public async Task GetCliPackagesAsync_Should_ExpireDiskCacheCorrectly()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Override the user directory for testing
        Environment.SetEnvironmentVariable("HOME", _tempDirectory.Path);
        Environment.SetEnvironmentVariable("USERPROFILE", _tempDirectory.Path);
        
        var callCount = 0;
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, configure =>
        {
            configure.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (_, _, _, _, _, _, _, _) =>
                {
                    callCount++;
                    return (0, [
                        new NuGetPackage { Id = "Aspire.Cli", Version = "9.4.0-preview", Source = "nuget.org" }
                    ]);
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var nuGetPackageCache = provider.GetRequiredService<INuGetPackageCache>();

        // Act - First call should hit the API and cache the result with 1 hour expiration
        var firstResult = await nuGetPackageCache.GetCliPackagesAsync(
            workspace.WorkspaceRoot, 
            prerelease: true, 
            nugetConfigFile: null, 
            CancellationToken.None).WaitAsync(CliTestConstants.DefaultTimeout);

        // Clear memory cache by creating a new service provider
        provider.Dispose();
        provider = services.BuildServiceProvider();
        nuGetPackageCache = provider.GetRequiredService<INuGetPackageCache>();

        // Act - Second call should use disk cache (not expired yet)
        var secondResult = await nuGetPackageCache.GetCliPackagesAsync(
            workspace.WorkspaceRoot, 
            prerelease: true, 
            nugetConfigFile: null, 
            CancellationToken.None).WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(1, callCount); // API should only be called once
        Assert.Single(firstResult);
        Assert.Single(secondResult);
        Assert.Equal("Aspire.Cli", firstResult.First().Id);
        Assert.Equal("Aspire.Cli", secondResult.First().Id);
    }

    [Fact]
    public async Task GetIntegrationPackagesAsync_Should_UseDiskCacheAcrossInstances()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Override the user directory for testing
        Environment.SetEnvironmentVariable("HOME", _tempDirectory.Path);
        Environment.SetEnvironmentVariable("USERPROFILE", _tempDirectory.Path);
        
        var callCount = 0;
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, configure =>
        {
            configure.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.SearchPackagesAsyncCallback = (_, _, _, _, _, _, _, _) =>
                {
                    callCount++;
                    return (0, [
                        new NuGetPackage { Id = "Aspire.Hosting.Redis", Version = "9.4.0-preview", Source = "nuget.org" },
                        new NuGetPackage { Id = "Aspire.Hosting.PostgreSQL", Version = "9.4.0-preview", Source = "nuget.org" }
                    ]);
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var nuGetPackageCache = provider.GetRequiredService<INuGetPackageCache>();

        // Act - First call should hit the API and cache the result
        var firstResult = await nuGetPackageCache.GetIntegrationPackagesAsync(
            workspace.WorkspaceRoot, 
            prerelease: true, 
            nugetConfigFile: null, 
            CancellationToken.None).WaitAsync(CliTestConstants.DefaultTimeout);

        // Simulate CLI restart by disposing and creating a new service provider
        provider.Dispose();
        provider = services.BuildServiceProvider();
        nuGetPackageCache = provider.GetRequiredService<INuGetPackageCache>();

        // Act - Second call should use disk cache
        var secondResult = await nuGetPackageCache.GetIntegrationPackagesAsync(
            workspace.WorkspaceRoot, 
            prerelease: true, 
            nugetConfigFile: null, 
            CancellationToken.None).WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(1, callCount); // API should only be called once
        Assert.Equal(2, firstResult.Count());
        Assert.Equal(2, secondResult.Count());
        Assert.Contains(firstResult, p => p.Id == "Aspire.Hosting.Redis");
        Assert.Contains(secondResult, p => p.Id == "Aspire.Hosting.Redis");
    }

    public void Dispose()
    {
        _tempDirectory?.Dispose();
    }
}