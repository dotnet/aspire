// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal interface IDnsResolver
{
    ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, CancellationToken cancellationToken = default);
    ValueTask<ServiceResult[]> ResolveServiceAsync(string name, CancellationToken cancellationToken = default);
}
