// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Aspire.Dashboard.Extensions;

namespace Aspire.Dashboard.Utils;

public static class VersionHelpers
{
    private static readonly Lazy<string?> s_cachedRuntimeVersion = new Lazy<string?>(GetRuntimeVersion);

    public static string? DashboardDisplayVersion { get; } = typeof(VersionHelpers).Assembly.GetDisplayVersion();

    public static string? RuntimeVersion => s_cachedRuntimeVersion.Value;

    private static string? GetRuntimeVersion()
    {
        var description = RuntimeInformation.FrameworkDescription;

        // Example inputs:
        // ".NET 8.0.3"
        // ".NET Core 3.1.32"
        // ".NET Framework 4.8.9032.0"

        int lastSpace = description.LastIndexOf(' ');
        if (lastSpace >= 0 && lastSpace < description.Length - 1)
        {
            return description.Substring(lastSpace + 1);
        }

        return null;
    }
}
