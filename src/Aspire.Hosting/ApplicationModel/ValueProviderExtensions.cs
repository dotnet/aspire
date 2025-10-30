// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal static class ValueProviderExtensions
{
    public static NetworkIdentifier GetNetworkIdentifier(this ValueProviderContext context)
    {
        return context?.Network ?? context?.Caller?.GetDefaultResourceNetwork() ?? KnownNetworkIdentifiers.LocalhostNetwork;
    }
}