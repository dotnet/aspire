// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.PackageChannels;

/// <summary>
/// Implementation of package channel service that provides access to predefined and dynamic package channels.
/// </summary>
internal sealed class PackageChannelService : IPackageChannelService
{
    private static readonly PackageChannel[] s_predefinedChannels =
    [
        new("stable", "https://api.nuget.org/v3/index.json", PackageChannelType.NuGetFeed),
        new("preview", "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json", PackageChannelType.NuGetFeed),
        new("daily", "https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-main/nuget/v3/index.json", PackageChannelType.NuGetFeed)
    ];

    private readonly string _hivesDirectory;

    public PackageChannelService()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _hivesDirectory = Path.Combine(homeDirectory, ".aspire", "hives");
    }

    public IEnumerable<PackageChannel> GetAllChannels()
    {
        // Return predefined channels first
        foreach (var channel in s_predefinedChannels)
        {
            yield return channel;
        }

        // Then return PR channels
        foreach (var prChannel in GetPrChannels())
        {
            yield return prChannel;
        }
    }

    public PackageChannel? GetChannelByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        // Check predefined channels first
        var predefinedChannel = s_predefinedChannels.FirstOrDefault(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        
        if (predefinedChannel is not null)
        {
            return predefinedChannel;
        }

        // Check PR channels
        return GetPrChannels().FirstOrDefault(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<PackageChannel> GetPrChannels()
    {
        if (!Directory.Exists(_hivesDirectory))
        {
            yield break;
        }

        foreach (var directory in Directory.GetDirectories(_hivesDirectory))
        {
            var directoryName = Path.GetFileName(directory);
            
            // Check if directory name is a valid PR number
            if (int.TryParse(directoryName, out var prNumber) && prNumber > 0)
            {
                yield return new PackageChannel($"pr-{prNumber}", directory, PackageChannelType.LocalDirectory);
            }
        }
    }
}