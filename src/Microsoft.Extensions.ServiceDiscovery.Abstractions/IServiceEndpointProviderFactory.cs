// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Creates <see cref="IServiceEndpointProvider"/> instances.
/// </summary>
public interface IServiceEndpointProviderFactory
{
    /// <summary>
    /// Tries to create an <see cref="IServiceEndpointProvider"/> instance for the specified <paramref name="query"/>.
    /// </summary>
    /// <param name="query">The service to create the provider for.</param>
    /// <param name="provider">The provider.</param>
    /// <returns><see langword="true"/> if the provider was created, <see langword="false"/> otherwise.</returns>
    bool TryCreateProvider(ServiceEndpointQuery query, [NotNullWhen(true)] out IServiceEndpointProvider? provider);
}
