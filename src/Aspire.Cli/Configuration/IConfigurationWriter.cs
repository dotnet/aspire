// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Configuration;

internal interface IConfigurationWriter
{
    /// <summary>
    /// Sets a configuration value in the appropriate .aspire/settings.json file.
    /// Creates the file and directory structure if they don't exist.
    /// </summary>
    /// <param name="key">The configuration key to set.</param>
    /// <param name="value">The configuration value to set.</param>
    /// <param name="isGlobal">If true, writes to $HOME/.aspire/settings.json; otherwise, writes to the nearest local .aspire/settings.json.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SetConfigurationAsync(string key, string value, bool isGlobal = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a configuration key from the appropriate .aspire/settings.json file.
    /// </summary>
    /// <param name="key">The configuration key to delete.</param>
    /// <param name="isGlobal">If true, deletes from $HOME/.aspire/settings.json; otherwise, deletes from the nearest local .aspire/settings.json.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the key was found and deleted, false if the key was not found.</returns>
    Task<bool> DeleteConfigurationAsync(string key, bool isGlobal = false, CancellationToken cancellationToken = default);
}