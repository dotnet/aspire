// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.NuGet;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Aspire.Cli.Packaging;

internal interface IPackagingService
{
    public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default);
}

internal class PackagingService(CliExecutionContext executionContext, INuGetPackageCache nuGetPackageCache, IFeatures features, IConfiguration configuration) : IPackagingService
{
    public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        var defaultChannel = PackageChannel.CreateImplicitChannel(nuGetPackageCache);
        
        var stableChannel = PackageChannel.CreateExplicitChannel("stable", PackageChannelQuality.Stable, new[]
        {
            new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
        }, nuGetPackageCache);

        var dailyChannel = PackageChannel.CreateExplicitChannel("daily", PackageChannelQuality.Prerelease, new[]
        {
            new PackageMapping("Aspire*", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json"),
            new PackageMapping("Microsoft.Extensions.ServiceDiscovery*", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json"),
            new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
        }, nuGetPackageCache);

        var prPackageChannels = new List<PackageChannel>();

        // Cannot use HiveDirectory.Exists here because it blows up on the
        // intermediate directory structure which may not exist in some
        // contexts (e.g. in our Codespace where we have the CLI on the 
        // path but not in the $HOME/.aspire/bin folder).
        if (executionContext.HivesDirectory.Exists)
        {
            var prHives = executionContext.HivesDirectory.GetDirectories();
            foreach (var prHive in prHives)
            {
                var prChannel = PackageChannel.CreateExplicitChannel(prHive.Name, PackageChannelQuality.Prerelease, new[]
                {
                    new PackageMapping("Aspire*", prHive.FullName),
                    new PackageMapping("Microsoft.Extensions.ServiceDiscovery*", prHive.FullName),
                    new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
                }, nuGetPackageCache);

                prPackageChannels.Add(prChannel);
            }
        }

        var channels = new List<PackageChannel>([defaultChannel, stableChannel, dailyChannel, ..prPackageChannels]);

        // Add staging channel if feature is enabled
        if (features.IsFeatureEnabled(KnownFeatures.StagingChannelEnabled, false))
        {
            var stagingChannel = CreateStagingChannel();
            if (stagingChannel is not null)
            {
                channels.Add(stagingChannel);
            }
        }

        return Task.FromResult<IEnumerable<PackageChannel>>(channels);
    }

    private PackageChannel? CreateStagingChannel()
    {
        var commitHash = GetCommitHashForStagingChannel();
        if (commitHash is null)
        {
            return null;
        }

        var stagingFeedUrl = $"https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-{commitHash}/nuget/v3/index.json";

        var stagingChannel = PackageChannel.CreateExplicitChannel("staging", PackageChannelQuality.Stable, new[]
        {
            new PackageMapping("Aspire*", stagingFeedUrl),
            new PackageMapping("Microsoft.Extensions.ServiceDiscovery*", stagingFeedUrl),
            new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
        }, nuGetPackageCache);

        return stagingChannel;
    }

    private string? GetCommitHashForStagingChannel()
    {
        // Check for test override first
        var overrideHash = configuration["overrideStagingHash"];
        if (!string.IsNullOrEmpty(overrideHash))
        {
            return overrideHash.Length >= 8 ? overrideHash[..8] : overrideHash;
        }

        // Extract from assembly version
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .OfType<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        if (informationalVersion is null)
        {
            return null;
        }

        var plusIndex = informationalVersion.IndexOf('+');
        if (plusIndex < 0 || plusIndex + 1 >= informationalVersion.Length)
        {
            return null;
        }

        var commitHash = informationalVersion[(plusIndex + 1)..];
        return commitHash.Length >= 8 ? commitHash[..8] : commitHash;
    }
}
