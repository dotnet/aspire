// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Orleans;

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
    /// <param name="configSectionPath">The name of the configuration section which this value is being added to.</param>
    void ConfigureResource<T>(IResourceBuilder<T> resourceBuilder, string configSectionPath) where T : IResourceWithEnvironment;
}
