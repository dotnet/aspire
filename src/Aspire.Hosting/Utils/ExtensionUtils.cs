// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Utils;

internal static class ExtensionUtils
{
    public static bool IsExtensionHost(IConfiguration configuration)
    {
        return configuration[KnownConfigNames.DebugSessionInfo] is not null;
    }
}
