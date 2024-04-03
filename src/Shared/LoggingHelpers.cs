// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

internal static class LoggingHelpers
{
    public static void WriteDashboardUrl(ILogger logger, string? dashboardUrls, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Token must be provided.");
        }

        if (StringUtils.TryGetUriFromDelimitedString(dashboardUrls, ";", out var firstDashboardUrl))
        {
            logger.LogInformation("Login to the dashboard at {DashboardUrl}", $"{firstDashboardUrl.GetLeftPart(UriPartial.Authority)}/login?t={token}");
        }
    }
}
