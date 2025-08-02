// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.PackageChannels;

/// <summary>
/// Service for managing and querying available package channels.
/// </summary>
internal interface IPackageChannelService
{
    /// <summary>
    /// Gets all available package channels.
    /// </summary>
    /// <returns>A collection of all available package channels.</returns>
    IEnumerable<PackageChannel> GetAllChannels();

    /// <summary>
    /// Looks up a package channel by name.
    /// </summary>
    /// <param name="name">The name of the channel to look up.</param>
    /// <returns>The package channel if found, otherwise null.</returns>
    PackageChannel? GetChannelByName(string name);

    /// <summary>
    /// Gets all PR channels available in the local hives directory.
    /// </summary>
    /// <returns>A collection of PR package channels.</returns>
    IEnumerable<PackageChannel> GetPrChannels();
}