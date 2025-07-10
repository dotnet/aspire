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
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

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
    public void GetOrderedApplications_MultipleUnparentedSpans_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var app2 = new OtlpApplication("app2", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-2", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

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
    public void GetOrderedApplications_ChildSpanAfterParentSpan_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var app2 = new OtlpApplication("app2", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

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
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var app2 = new OtlpApplication("app2", "instance", uninstrumentedPeer: false, context);
        var app3 = new OtlpApplication("app3", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app3, trace, scope, spanId: "1-1-1", parentSpanId: "1-1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app3, trace, scope, spanId: "1-2", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));

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

    [Fact]
    public void GetOrderedApplications_HasUninstrumentedPeer_AddedToResults()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var app2 = new OtlpApplication("app2", "instance", uninstrumentedPeer: false, context);
        var app3 = new OtlpApplication("app3", "instance", uninstrumentedPeer: true, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc), uninstrumentedPeer: app3));

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
            },
            g =>
            {
                Assert.Equal(app3, g.Application);
            });
    }
}
