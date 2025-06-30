// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Aspire.Cli.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Utils;

public class CliUpdateNotificationServiceTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task NotifyIfUpdateAvailableAsync_WithNewerStableVersion_DoesNotThrow()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        
        // Replace the NuGetPackageCache with our test implementation
        services.AddSingleton<INuGetPackageCache, TestNuGetPackageCache>();
        services.AddSingleton<ICliUpdateNotififier, CliUpdateNotififier>();
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ICliUpdateNotififier>();
        
        // Mock packages with a newer stable version
        var nugetCache = provider.GetRequiredService<INuGetPackageCache>() as TestNuGetPackageCache;
        nugetCache?.SetMockCliPackages([
            new NuGetPackage { Id = "Aspire.Cli", Version = "9.0.0", Source = "nuget.org" }
        ]);

        // Act & Assert (should not throw)
        await service.NotifyIfUpdateAvailableAsync(workspace.WorkspaceRoot);
    }

    [Fact]
    public async Task NotifyIfUpdateAvailableAsync_WithEmptyPackages_DoesNotThrow()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        
        // Replace the NuGetPackageCache with our test implementation
        services.AddSingleton<INuGetPackageCache, TestNuGetPackageCache>();
        services.AddSingleton<ICliUpdateNotififier, CliUpdateNotififier>();
        
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ICliUpdateNotififier>();

        // Act & Assert (should not throw)
        await service.NotifyIfUpdateAvailableAsync(workspace.WorkspaceRoot);
    }
}

internal sealed class TestNuGetPackageCache : INuGetPackageCache
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