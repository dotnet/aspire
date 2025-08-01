// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.PackageChannels;

/// <summary>
/// Represents a package channel with its name and backing location.
/// </summary>
internal sealed record PackageChannel(string Name, string Location, PackageChannelType Type)
{
    /// <summary>
    /// Gets a display name for the channel.
    /// </summary>
    public string DisplayName => Name switch
    {
        "stable" => "Stable (nuget.org)",
        "preview" => "Preview (Azure DevOps)",
        "daily" => "Daily (CI builds)",
        _ when Name.StartsWith("pr-") => $"PR {Name[3..]} (Local)",
        _ => Name
    };
}

/// <summary>
/// Defines the type of package channel.
/// </summary>
internal enum PackageChannelType
{
    /// <summary>
    /// A NuGet feed URL.
    /// </summary>
    NuGetFeed,

    /// <summary>
    /// A local directory path.
    /// </summary>
    LocalDirectory
}