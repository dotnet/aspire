// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal interface IDashboardTokenProvider
{
    public string BrowserToken { get; }
    public string OltpToken { get; }
    public string ResourceServerToken { get; }
}

internal class DashboardTokenProvider : IDashboardTokenProvider
{
    public DashboardTokenProvider(IOptions<DashboardAuthenticationOptions> dashboardAuthenticationOptions)
    {
        BrowserToken = dashboardAuthenticationOptions.Value.BrowserToken ?? GenerateToken();
        OltpToken = GenerateToken();
        ResourceServerToken = GenerateToken();
    }

    private static string GenerateToken()
    {
        var rawToken = PasswordGenerator.Generate(24, true, true, true, true, 0, 0, 0, 0);
        var rawTokenBytes = Encoding.UTF8.GetBytes(rawToken);
        var encodedToken = Convert.ToHexString(rawTokenBytes).ToLower();
        return encodedToken;
    }

    public string BrowserToken { get; init; }
    public string OltpToken { get; init; }
    public string ResourceServerToken { get; init; }
}
