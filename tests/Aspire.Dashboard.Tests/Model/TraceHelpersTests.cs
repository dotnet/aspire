// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class TraceHelpersTests
{
    [Fact]
    public void GetOrderedApplications_SingleSpan_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance1", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(CreateSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

        // Act
        var results = TraceHelpers.GetOrderedApplications(trace);

        // Assert
        Assert.Collection(results,
            g =>
            {
                Assert.Equal(app1, g.Application);
            });
    }

    [Fact]
    public void GetOrderedApplications_ChildSpanAfterParentSpan_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var app2 = new OtlpApplication("app2", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(CreateSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(CreateSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

        // Act
        var results = TraceHelpers.GetOrderedApplications(trace);

        // Assert
        Assert.Collection(results,
            g =>
            {
                Assert.Equal(app1, g.Application);
            },
            g =>
            {
                Assert.Equal(app2, g.Application);
            });
    }

    [Fact]
    public void GetOrderedApplications_ChildSpanDifferentStartTime_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var app2 = new OtlpApplication("app2", "instance", context);
        var app3 = new OtlpApplication("app3", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(CreateSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(CreateSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc)));
        trace.AddSpan(CreateSpan(app3, trace, scope, spanId: "1-1-1", parentSpanId: "1-1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(CreateSpan(app3, trace, scope, spanId: "1-2", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));

        // Act
        var results = TraceHelpers.GetOrderedApplications(trace);

        // Assert
        Assert.Collection(results,
            g =>
            {
                Assert.Equal(app1, g.Application);
            },
            g =>
            {
                Assert.Equal(app3, g.Application);
            },
            g =>
            {
                Assert.Equal(app2, g.Application);
            });
    }

    private static OtlpSpan CreateSpan(OtlpApplication app, OtlpTrace trace, OtlpScope scope, string spanId, string? parentSpanId, DateTime startDate)
    {
        return new OtlpSpan(app.GetView([]), trace, scope)
        {
            Attributes = [],
            BackLinks = [],
            EndTime = DateTime.MaxValue,
            Events = [],
            Kind = OtlpSpanKind.Unspecified,
            Links = [],
            Name = "Test",
            ParentSpanId = parentSpanId,
            SpanId = spanId,
            StartTime = startDate,
            State = null,
            Status = OtlpSpanStatusCode.Unset,
            StatusMessage = null
        };
    }
}
