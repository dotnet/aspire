// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

[Flags]
internal enum QueryFlags : ushort
{
    IsCheckingDisabled = 0x0010,
    IsAuthenticData = 0x0020,
    RecursionAvailable = 0x0080,
    RecursionDesired = 0x0100,
    ResultTruncated = 0x0200,
    HasAuthorityAnswer = 0x0400,
    HasResponse = 0x8000,
}
