// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;

namespace Aspire.Dashboard.Utils;

public static class VersionHelpers
{
    public static string? DashboardDisplayVersion { get; } = typeof(VersionHelpers).Assembly.GetDisplayVersion();
}
