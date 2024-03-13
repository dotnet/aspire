// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// <see cref="IServiceEndPointResolverProvider"/> implementation that resolves services using <see cref="IConfiguration"/>.
/// </summary>
internal sealed class ConfigurationServiceEndPointResolverProvider(
    IConfiguration configuration,
    IOptions<ConfigurationServiceEndPointResolverOptions> options,
    ILogger<ConfigurationServiceEndPointResolver> logger,
    ServiceNameParser parser) : IServiceEndPointResolverProvider
{
    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointProvider? resolver)
    {
        resolver = new ConfigurationServiceEndPointResolver(serviceName, configuration, logger, options, parser);
        return true;
    }
}
