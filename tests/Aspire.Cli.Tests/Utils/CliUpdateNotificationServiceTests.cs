// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;
using System.Reflection;

namespace Aspire.Cli.Tests.Utils;

public class CliUpdateNotificationServiceTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task NotifyIfUpdateAvailableAsync_WithNewerStableVersion_ShowsNotification()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var console = new TestConsole();
        var services = CreateServices(workspace, console);
        var service = services.GetRequiredService<ICliUpdateNotificationService>();
        
        // Mock packages with a newer stable version
        var nugetCache = services.GetRequiredService<INuGetPackageCache>() as TestNuGetPackageCache;
        nugetCache?.SetMockCliPackages([
            new NuGetPackage { Id = "Aspire.Cli", Version = "9.0.0", Source = "nuget.org" }
        ]);

        // Act
        await service.NotifyIfUpdateAvailableAsync(workspace.WorkspaceRoot);

        // Assert
        var output = console.Output;
        Assert.Contains("A new version of the Aspire CLI is available", output);
        Assert.Contains("9.0.0", output);
        Assert.Contains("https://aka.ms/aspire/update-cli", output);
    }

    [Fact]
    public async Task NotifyIfUpdateAvailableAsync_WithNoNewerVersion_ShowsNoNotification()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var console = new TestConsole();
        var services = CreateServices(workspace, console);
        var service = services.GetRequiredService<ICliUpdateNotificationService>();
        
        // Mock packages with same or older version
        var nugetCache = services.GetRequiredService<INuGetPackageCache>() as TestNuGetPackageCache;
        nugetCache?.SetMockCliPackages([
            new NuGetPackage { Id = "Aspire.Cli", Version = "1.0.0", Source = "nuget.org" }
        ]);

        // Act
        await service.NotifyIfUpdateAvailableAsync(workspace.WorkspaceRoot);

        // Assert
        var output = console.Output;
        Assert.DoesNotContain("A new version of the Aspire CLI is available", output);
    }

    [Fact]
    public async Task NotifyIfUpdateAvailableAsync_WithEmptyPackages_ShowsNoNotification()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var console = new TestConsole();
        var services = CreateServices(workspace, console);
        var service = services.GetRequiredService<ICliUpdateNotificationService>();
        
        // Mock empty packages
        var nugetCache = services.GetRequiredService<INuGetPackageCache>() as TestNuGetPackageCache;
        nugetCache?.SetMockCliPackages([]);

        // Act
        await service.NotifyIfUpdateAvailableAsync(workspace.WorkspaceRoot);

        // Assert
        var output = console.Output;
        Assert.DoesNotContain("A new version of the Aspire CLI is available", output);
    }

    private static IServiceProvider CreateServices(TemporaryWorkspace workspace, IAnsiConsole console)
    {
        var services = CliTestHelper.CreateServiceCollection(workspace, TestOutputHelper.Create(), options =>
        {
            options.ServiceCollectionConfigurer = (sc) =>
            {
                sc.AddSingleton(console);
                sc.AddSingleton<INuGetPackageCache, TestNuGetPackageCache>();
                sc.AddSingleton<ICliUpdateNotificationService, CliUpdateNotificationService>();
            };
        });
     
        return services.BuildServiceProvider();
    }
}

internal class TestNuGetPackageCache : INuGetPackageCache
{
    private IEnumerable<NuGetPackage> _cliPackages = [];

    public void SetMockCliPackages(IEnumerable<NuGetPackage> packages)
    {
        _cliPackages = packages;
    }

    public Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        return Task.FromResult(Enumerable.Empty<NuGetPackage>());
    }

    public Task<IEnumerable<NuGetPackage>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        return Task.FromResult(Enumerable.Empty<NuGetPackage>());
    }

    public Task<IEnumerable<NuGetPackage>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        return Task.FromResult(_cliPackages);
    }
}