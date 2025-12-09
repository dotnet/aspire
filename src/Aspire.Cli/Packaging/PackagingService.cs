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
        }, nuGetPackageCache, cliDownloadBaseUrl: "https://aka.ms/dotnet/9/aspire/ga/daily");

        var dailyChannel = PackageChannel.CreateExplicitChannel("daily", PackageChannelQuality.Prerelease, new[]
        {
            new PackageMapping("Aspire*", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json"),
            new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
        }, nuGetPackageCache, cliDownloadBaseUrl: "https://aka.ms/dotnet/9/aspire/daily");

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
                    new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
                }, nuGetPackageCache);

                prPackageChannels.Add(prChannel);
            }
        }

        var channels = new List<PackageChannel>([defaultChannel, stableChannel]);

        // Add staging channel if feature is enabled (after stable, before daily)
        if (features.Enabled<StagingChannelEnabledFeature>())
        {
            var stagingChannel = CreateStagingChannel();
            if (stagingChannel is not null)
            {
                channels.Add(stagingChannel);
            }
        }

        // Add daily and PR channels after staging
        channels.Add(dailyChannel);
        channels.AddRange(prPackageChannels);

        return Task.FromResult<IEnumerable<PackageChannel>>(channels);
    }

    private PackageChannel? CreateStagingChannel()
    {
        var stagingFeedUrl = GetStagingFeedUrl();
        if (stagingFeedUrl is null)
        {
            return null;
        }

        var stagingQuality = GetStagingQuality();

        var stagingChannel = PackageChannel.CreateExplicitChannel("staging", stagingQuality, new[]
        {
            new PackageMapping("Aspire*", stagingFeedUrl),
            new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
        }, nuGetPackageCache, configureGlobalPackagesFolder: true, cliDownloadBaseUrl: "https://aka.ms/dotnet/9/aspire/rc/daily");

        return stagingChannel;
    }

    private string? GetStagingFeedUrl()
    {
        // Check for configuration override first
        var overrideFeed = configuration["overrideStagingFeed"];
        if (!string.IsNullOrEmpty(overrideFeed))
        {
            // Validate that the override URL is well-formed
            if (Uri.TryCreate(overrideFeed, UriKind.Absolute, out var uri) && 
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                return overrideFeed;
            }
            // Invalid URL, fall through to default behavior
        }

        // Extract commit hash from assembly version to build staging feed URL
        // Staging feed URL template: https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-{commitHash}/nuget/v3/index.json
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
        var truncatedHash = commitHash.Length >= 8 ? commitHash[..8] : commitHash;
        
        return $"https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-{truncatedHash}/nuget/v3/index.json";
    }

    private PackageChannelQuality GetStagingQuality()
    {
        // Check for configuration override
        var overrideQuality = configuration["overrideStagingQuality"];
        if (!string.IsNullOrEmpty(overrideQuality))
        {
            // Try to parse the quality value (case-insensitive)
            if (Enum.TryParse<PackageChannelQuality>(overrideQuality, ignoreCase: true, out var quality))
            {
                return quality;
            }
        }

        // Default to Stable if not specified or invalid
        return PackageChannelQuality.Stable;
    }
}
