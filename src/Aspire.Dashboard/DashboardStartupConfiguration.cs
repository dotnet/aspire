// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Authentication;

namespace Aspire.Dashboard;

public sealed class DashboardStartupConfiguration
{
    public required Uri[] BrowserUris { get; init; }
    public required Uri OtlpUri { get; init; }
    public required OtlpAuthMode OtlpAuthMode { get; init; }
    public required string? OtlpApiKey { get; init; }
}
