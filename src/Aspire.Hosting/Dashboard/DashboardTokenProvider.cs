// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal interface IDashboardTokenProvider
{
    public string BrowserToken { get; }
    public string DashboardOtlpToken { get; }
    public string ResourceServerToken { get; }
}

internal class DashboardTokenProvider : IDashboardTokenProvider
{
    public DashboardTokenProvider(ILogger<DashboardTokenProvider> logger, IOptions<TransportOptions> transportOptions)
    {
        if (transportOptions.Value.BrowserToken is not { } browserToken)
        {
            logger.LogDebug("Browser token was not supplied from environment, will be automatically generated.");
            BrowserToken = GenerateToken();
        }
        else
        {
            logger.LogDebug("Browser token was supplied from environment, will not be generated.");
            BrowserToken = browserToken;
        }

        DashboardOtlpToken = GenerateToken();
        ResourceServerToken = GenerateToken();
    }

    private static string GenerateToken()
    {
        var rawToken = PasswordGenerator.Generate(24);
        var rawTokenBytes = Encoding.UTF8.GetBytes(rawToken);

#if NET9_0_OR_GREATER
        var encodedToken = Convert.ToHexStringLower(rawTokenBytes);
#else
        var encodedToken = Convert.ToHexString(rawTokenBytes).ToLower();
#endif

        return encodedToken;
    }

    public string BrowserToken { get; }
    public string DashboardOtlpToken { get; }
    public string ResourceServerToken { get; }
}
