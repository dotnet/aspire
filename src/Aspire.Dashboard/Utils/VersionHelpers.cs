// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Aspire.Dashboard.Extensions;

namespace Aspire.Dashboard.Utils;

public static class VersionHelpers
{
    private static readonly Lazy<Version?> s_cachedRuntimeVersion = new Lazy<Version?>(GetRuntimeVersion);

    public static string? DashboardDisplayVersion { get; } = typeof(VersionHelpers).Assembly.GetDisplayVersion();

    public static Version? RuntimeVersion => s_cachedRuntimeVersion.Value;

    private static Version? GetRuntimeVersion()
    {
        var description = RuntimeInformation.FrameworkDescription;

        // Examples: ".NET 8.0.3", ".NET 7.0.16", ".NET Core 3.1.32"
        var parts = description.Split(' ');
        if (parts.Length >= 2)
        {
            if (Version.TryParse(parts[1], out var v))
            {
                return v;
            }
        }
        return null;
    }
}
