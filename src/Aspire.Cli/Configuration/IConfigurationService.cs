// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Configuration;

internal interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value from the nearest .aspire/settings.json file.
    /// </summary>
    /// <param name="key">The configuration key to retrieve.</param>
    /// <returns>The configuration value if found, otherwise null.</returns>
    string? GetConfiguration(string key);

    /// <summary>
    /// Sets a configuration value in the nearest .aspire/settings.json file.
    /// Creates the file and directory structure if they don't exist.
    /// </summary>
    /// <param name="key">The configuration key to set.</param>
    /// <param name="value">The configuration value to set.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SetConfigurationAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration key-value pairs from the nearest .aspire/settings.json file.
    /// </summary>
    /// <returns>A dictionary of configuration key-value pairs.</returns>
    IReadOnlyDictionary<string, string> GetAllConfiguration();

    /// <summary>
    /// Deletes a configuration key from the nearest .aspire/settings.json file.
    /// </summary>
    /// <param name="key">The configuration key to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the key was found and deleted, false if the key was not found.</returns>
    Task<bool> DeleteConfigurationAsync(string key, CancellationToken cancellationToken = default);
}