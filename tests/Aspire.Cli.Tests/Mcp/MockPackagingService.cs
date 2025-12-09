// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Shared;

namespace Aspire.Cli.Tests.Mcp;

internal sealed class MockPackagingService : IPackagingService
{
    private readonly NuGetPackageCli[] _packages;

    public MockPackagingService(NuGetPackageCli[]? packages = null)
    {
        _packages = packages ?? [];
    }

    public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        var nugetCache = new MockNuGetPackageCache(_packages);
        var channels = new[] { PackageChannel.CreateImplicitChannel(nugetCache) };
        return Task.FromResult<IEnumerable<PackageChannel>>(channels);
    }
}

internal sealed class MockNuGetPackageCache : INuGetPackageCache
{
    private readonly NuGetPackageCli[] _packages;

    public MockNuGetPackageCache(NuGetPackageCli[]? packages = null)
    {
        _packages = packages ?? [];
    }

    public Task<IEnumerable<NuGetPackageCli>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<NuGetPackageCli>>([]);

    public Task<IEnumerable<NuGetPackageCli>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<NuGetPackageCli>>(_packages);

    public Task<IEnumerable<NuGetPackageCli>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<NuGetPackageCli>>([]);

    public Task<IEnumerable<NuGetPackageCli>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, bool useCache, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<NuGetPackageCli>>([]);
}

internal static class TestExecutionContextFactory
{
    public static CliExecutionContext CreateTestContext()
    {
        return new CliExecutionContext(
            new DirectoryInfo(Path.GetTempPath()),
            new DirectoryInfo(Path.Combine(Path.GetTempPath(), "hives")),
            new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cache")),
            new DirectoryInfo(Path.Combine(Path.GetTempPath(), "sdks")));
    }
}

internal sealed class MockAuxiliaryBackchannelMonitor : IAuxiliaryBackchannelMonitor
{
    private readonly Dictionary<string, AppHostConnection> _connections = new();

    public IReadOnlyDictionary<string, AppHostConnection> Connections => _connections;

    public string? SelectedAppHostPath { get; set; }

    public AppHostConnection? SelectedConnection => null;

    public IReadOnlyList<AppHostConnection> GetConnectionsForWorkingDirectory(DirectoryInfo workingDirectory)
    {
        // Return empty list by default (no in-scope AppHosts)
        return Array.Empty<AppHostConnection>();
    }
}
