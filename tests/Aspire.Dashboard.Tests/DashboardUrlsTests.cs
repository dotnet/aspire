// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class DashboardUrlsTests
{
    [Fact]
    public void ConsoleLogsUrl_HtmlValues_CorrectlyEscaped()
    {
        Assert.Equal("/consolelogs/resource/resource!%40%23", DashboardUrls.ConsoleLogsUrl(resource: "resource!@#"));
    }

    [Fact]
    public void StructuredLogsUrl_HtmlValues_CorrectlyEscaped()
    {
        var url = DashboardUrls.StructuredLogsUrl(
            resource: "resource!@#",
            logLevel: "error",
            filters: "test:contains:value",
            traceId: "!@#",
            spanId: "!@#");

        Assert.Equal("/structuredlogs/resource/resource!%40%23?logLevel=error&filters=test:contains:value&traceId=!@%23&spanId=!@%23", url);
    }

    [Fact]
    public void TracesUrl_HtmlValues_CorrectlyEscaped()
    {
        Assert.Equal("/traces/resource/resource!%40%23", DashboardUrls.TracesUrl(resource: "resource!@#"));
    }

    [Fact]
    public void TraceDetailUrl_HtmlValues_CorrectlyEscaped()
    {
        Assert.Equal("/traces/detail/traceId!%40%23", DashboardUrls.TraceDetailUrl(traceId: "traceId!@#"));
    }

    [Fact]
    public void MetricsUrl_HtmlValues_CorrectlyEscaped()
    {
        var url = DashboardUrls.MetricsUrl(
            resource: "resource!@#",
            meter: "meter!@#",
            instrument: "meter!@#",
            duration: 10,
            view: "table");

        Assert.Equal("/metrics/resource/resource!%40%23?meter=meter!@%23&instrument=meter!@%23&duration=10&view=table", url);
    }
}
