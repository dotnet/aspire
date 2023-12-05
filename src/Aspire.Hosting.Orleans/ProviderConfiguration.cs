// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Configuration for an Orleans provider.
/// </summary>
public interface IProviderConfiguration
{
    /// <summary>
    /// Configures the provided resource.
    /// </summary>
    /// <typeparam name="T">The underlying resource builder type.</typeparam>
    /// <param name="resourceBuilder">The resource builder.</param>
    /// <param name="configurationSectionName">The name of the configuration section which this value is being added to.</param>
    void ConfigureResource<T>(IResourceBuilder<T> resourceBuilder, string configurationSectionName) where T : IResourceWithEnvironment;
}

/// <summary>
/// Configuration for an Orleans provider.
/// </summary>
internal sealed class ProviderConfiguration(string providerType, string? connectionName = null, IResourceBuilder<IResourceWithConnectionString>? resource = null) : IProviderConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProviderConfiguration"/>.
    /// </summary>
    /// <param name="resourceBuilder">The resource which this provider configuration represents.</param>
    /// <returns>The new provider configuration.</returns>
    public static ProviderConfiguration Create(IResourceBuilder<IResourceWithConnectionString> resourceBuilder)
        => new( resourceBuilder.Resource.GetType().Name, resourceBuilder.Resource.Name, resourceBuilder);

    /// <summary>
    /// Configures the provided resource.
    /// </summary>
    /// <typeparam name="T">The underlying resource builder type.</typeparam>
    /// <param name="resourceBuilder">The resource builder.</param>
    /// <param name="configurationSectionName">The name of the configuration section which this value is being added to.</param>
    public void ConfigureResource<T>(IResourceBuilder<T> resourceBuilder, string configurationSectionName) where T : IResourceWithEnvironment
    {
        resourceBuilder.WithEnvironment($"{configurationSectionName}__ProviderType", providerType);
        if (!string.IsNullOrEmpty(connectionName))
        {
            resourceBuilder.WithEnvironment($"{configurationSectionName}__ConnectionName", connectionName);
        }

        if (resource is not null)
        {
            resourceBuilder.WithReference(resource);
        }
    }
}
