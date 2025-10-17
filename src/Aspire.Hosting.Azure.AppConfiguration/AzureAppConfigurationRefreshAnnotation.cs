// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppConfiguration;

/// <summary>
/// Annotation that stores refresh configuration for Azure App Configuration.
/// </summary>
/// <param name="refreshKey">The configuration key to watch for changes.</param>
/// <param name="refreshIntervalInSeconds">The refresh interval in seconds.</param>
internal sealed class AzureAppConfigurationRefreshAnnotation(string refreshKey, int refreshIntervalInSeconds) : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration key to watch for changes.
    /// </summary>
    public string RefreshKey { get; } = refreshKey;

    /// <summary>
    /// Gets the refresh interval in seconds.
    /// </summary>
    public int RefreshIntervalInSeconds { get; } = refreshIntervalInSeconds;
}
