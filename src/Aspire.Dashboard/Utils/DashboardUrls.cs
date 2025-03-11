// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace Aspire.Dashboard.Utils;

internal static class DashboardUrls
{
    public const string ResourcesBasePath = "";
    public const string ConsoleLogBasePath = "consolelogs";
    public const string MetricsBasePath = "metrics";
    public const string StructuredLogsBasePath = "structuredlogs";
    public const string TracesBasePath = "traces";
    public const string LoginBasePath = "login";

    public static string ResourcesUrl(string? resource = null, string? view = null)
    {
        var url = $"/{ResourcesBasePath}";
        if (resource != null)
        {
            url = QueryHelpers.AddQueryString(url, "resource", resource);
        }
        if (view != null)
        {
            url = QueryHelpers.AddQueryString(url, "view", view);
        }

        return url;
    }

    public static string ConsoleLogsUrl(string? resource = null)
    {
        var url = $"/{ConsoleLogBasePath}";
        if (resource != null)
        {
            url += $"/resource/{Uri.EscapeDataString(resource)}";
        }

        return url;
    }

    public static string MetricsUrl(string? resource = null, string? meter = null, string? instrument = null, int? duration = null, string? view = null)
    {
        var url = $"/{MetricsBasePath}";
        if (resource != null)
        {
            url += $"/resource/{Uri.EscapeDataString(resource)}";
        }
        if (meter is not null)
        {
            // Meter and instrument must be querystring parameters because it's valid for the name to contain forward slashes.
            url = QueryHelpers.AddQueryString(url, "meter", meter);
            if (instrument is not null)
            {
                url = QueryHelpers.AddQueryString(url, "instrument", instrument);
            }
        }
        if (duration != null)
        {
            url = QueryHelpers.AddQueryString(url, "duration", duration.Value.ToString(CultureInfo.InvariantCulture));
        }
        if (view != null)
        {
            url = QueryHelpers.AddQueryString(url, "view", view);
        }

        return url;
    }

    public static string StructuredLogsUrl(string? resource = null, string? logLevel = null, string? filters = null, string? traceId = null, string? spanId = null)
    {
        var url = $"/{StructuredLogsBasePath}";
        if (resource != null)
        {
            url += $"/resource/{Uri.EscapeDataString(resource)}";
        }
        if (logLevel != null)
        {
            url = QueryHelpers.AddQueryString(url, "logLevel", logLevel);
        }
        if (filters != null)
        {
            // Filters contains : and + characters. These are escaped when they're not needed to,
            // which makes the URL harder to read. Consider having a custom method for appending
            // query string here that uses an encoder that doesn't encode those characters.
            url = QueryHelpers.AddQueryString(url, "filters", filters);
        }
        if (traceId != null)
        {
            url = QueryHelpers.AddQueryString(url, "traceId", traceId);
        }
        if (spanId != null)
        {
            url = QueryHelpers.AddQueryString(url, "spanId", spanId);
        }

        return url;
    }

    public static string TracesUrl(string? resource = null, string? filters = null)
    {
        var url = $"/{TracesBasePath}";
        if (resource != null)
        {
            url += $"/resource/{Uri.EscapeDataString(resource)}";
        }
        if (filters != null)
        {
            // Filters contains : and + characters. These are escaped when they're not needed to,
            // which makes the URL harder to read. Consider having a custom method for appending
            // query string here that uses an encoder that doesn't encode those characters.
            url = QueryHelpers.AddQueryString(url, "filters", filters);
        }

        return url;
    }

    public static string TraceDetailUrl(string traceId, string? spanId = null)
    {
        var url = $"/{TracesBasePath}/detail/{Uri.EscapeDataString(traceId)}";
        if (spanId != null)
        {
            url = QueryHelpers.AddQueryString(url, "spanId", spanId);
        }

        return url;
    }

    public static string LoginUrl(string? returnUrl = null, string? token = null)
    {
        var url = $"/{LoginBasePath}";
        if (returnUrl != null)
        {
            url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
        }
        if (token != null)
        {
            url = QueryHelpers.AddQueryString(url, "t", token);
        }

        return url;
    }

    public static string SetLanguageUrl(string language, string redirectUrl)
    {
        var url = "/api/set-language";
        url = QueryHelpers.AddQueryString(url, "language", language);
        url = QueryHelpers.AddQueryString(url, "redirectUrl", redirectUrl);

        return url;
    }
}
