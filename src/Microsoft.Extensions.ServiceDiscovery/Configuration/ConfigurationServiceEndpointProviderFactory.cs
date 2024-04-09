// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Configuration;

/// <summary>
/// <see cref="IServiceEndpointProviderFactory"/> implementation that resolves services using <see cref="IConfiguration"/>.
/// </summary>
internal sealed class ConfigurationServiceEndpointProviderFactory(
    IConfiguration configuration,
    IOptions<ConfigurationServiceEndpointProviderOptions> options,
    IOptions<ServiceDiscoveryOptions> serviceDiscoveryOptions,
    ILogger<ConfigurationServiceEndpointProvider> logger) : IServiceEndpointProviderFactory
{
    /// <inheritdoc/>
    public bool TryCreateProvider(ServiceEndpointQuery query, [NotNullWhen(true)] out IServiceEndpointProvider? provider)
    {
        provider = new ConfigurationServiceEndpointProvider(query, configuration, logger, options, serviceDiscoveryOptions);
        return true;
    }
}
