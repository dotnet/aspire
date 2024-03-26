// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Dashboard;

internal interface IDashboardTokenProvider
{
    public string BrowserToken { get; }
    public string OltpToken { get; }
    public string ResourceServerToken { get; }
}

internal class DashboardTokenProvider : IDashboardTokenProvider
{
    public DashboardTokenProvider(IConfiguration configuration)
    {
        BrowserToken = GenerateToken(configuration[KnownEnvironmentVariables.BrowserToken]);
        OltpToken = GenerateToken();
        ResourceServerToken = GenerateToken();
    }

    private static string GenerateToken(string? overrideValue = null)
    {
        var rawToken = overrideValue ?? PasswordGenerator.Generate(24, true, true, true, true, 6, 6, 6, 6);
        var rawTokenBytes = Encoding.UTF8.GetBytes(rawToken);
        var encodedToken = Convert.ToHexString(rawTokenBytes);
        return encodedToken;
    }

    public string BrowserToken { get; init; }
    public string OltpToken { get; init; }
    public string ResourceServerToken { get; init; }
}
