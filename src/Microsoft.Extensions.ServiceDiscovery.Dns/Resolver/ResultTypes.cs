// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal record struct AddressResult(DateTime ExpiresAt, IPAddress Address);

internal record struct ServiceResult(DateTime ExpiresAt, int Priority, int Weight, int Port, string Target, AddressResult[] Addresses);
