// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Proto.Common.V1;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class SpanWaterfallViewModelTests
{
    [Fact]
    public void Create_HasChildren_ChildrenPopulated()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var app2 = new OtlpApplication("app2", "instance", uninstrumentedPeer: false, context);

        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc)));

        // Act
        var vm = SpanWaterfallViewModel.Create(trace, [], new SpanWaterfallViewModel.TraceDetailState([], []));

        // Assert
        Assert.Collection(vm,
            e =>
            {
                Assert.Equal("1", e.Span.SpanId);
                Assert.Equal("1-1", Assert.Single(e.Children).Span.SpanId);
            },
            e =>
            {
                Assert.Equal("1-1", e.Span.SpanId);
                Assert.Empty(e.Children);
            });
    }

    [Fact]
    public void Create_RootSpanZeroDuration_ZeroPercentage()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var app1View = new OtlpApplicationView(app1, new RepeatedField<KeyValue>());

        var date = new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "31", parentSpanId: null, startDate: date, endDate: date));
        var log = new OtlpLogEntry(TelemetryTestHelpers.CreateLogRecord(traceId: trace.TraceId, spanId: "1"), app1View, scope, context);

        // Act
        var vm = SpanWaterfallViewModel.Create(trace, [log], new SpanWaterfallViewModel.TraceDetailState([], []));

        // Assert
        Assert.Collection(vm,
            e =>
            {
                Assert.Equal("31", e.Span.SpanId);
                Assert.Equal(0, e.LeftOffset);
                Assert.Equal(0, e.Width);

                var spanLog = Assert.Single(e.SpanLogs);
                Assert.Equal(0, spanLog.LeftOffset);
            });
    }

    [Fact]
    public void Create_OutgoingPeers_BrowserLink()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var app2 = new OtlpApplication("app2", "instance", uninstrumentedPeer: false, context);

        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc), kind: OtlpSpanKind.Client, attributes: [KeyValuePair.Create("http.url", "http://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag"), KeyValuePair.Create("server.address", "localhost")]));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "2", parentSpanId: null, startDate: new DateTime(2001, 2, 1, 1, 1, 2, DateTimeKind.Utc), kind: OtlpSpanKind.Client));

        // Act
        var vm = SpanWaterfallViewModel.Create(trace, [], new SpanWaterfallViewModel.TraceDetailState([new BrowserLinkOutgoingPeerResolver()], []));

        // Assert
        Assert.Collection(vm,
            e =>
            {
                Assert.Equal("1", e.Span.SpanId);
                Assert.Equal("Browser Link", e.UninstrumentedPeer);
            },
            e =>
            {
                Assert.Equal("2", e.Span.SpanId);
                Assert.Null(e.UninstrumentedPeer);
            });
    }

    [Theory]
    [InlineData("1234", true)]  // Matches span ID
    [InlineData("app1", true)]  // Matches application name
    [InlineData("Test", true)]  // Matches display summary
    [InlineData("peer-service", true)]  // Matches uninstrumented peer
    [InlineData("nonexistent", false)]  // Doesn't match anything
    public void MatchesFilter_VariousCases_ReturnsExpected(string filter, bool expected)
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);

        // Create a span with an attribute that simulates uninstrumented peer
        var attributes = new[]
        {
            new KeyValuePair<string, string>("peer.service", "peer-service")
        };

        var span = TelemetryTestHelpers.CreateOtlpSpan(
            app,
            trace,
            scope,
            spanId: "12345",
            parentSpanId: null,
            startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc),
            attributes: attributes,
            statusCode: OtlpSpanStatusCode.Unset,
            statusMessage: null,
            kind: OtlpSpanKind.Client);

        trace.AddSpan(span);

        var vm = SpanWaterfallViewModel.Create(
            trace,
            [],
            new SpanWaterfallViewModel.TraceDetailState([], [])).First();

        // Act
        var result = vm.MatchesFilter(filter, a => a.Application.ApplicationName, out _);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MatchesFilter_ParentSpanIncludedWhenChildMatched()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        var parentSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "parent", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc));
        var childSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "child", parentSpanId: "parent", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc));
        trace.AddSpan(parentSpan);
        trace.AddSpan(childSpan);

        var vms = SpanWaterfallViewModel.Create(trace, [], new SpanWaterfallViewModel.TraceDetailState([], []));
        var parent = vms[0];
        var child = vms[1];

        // Act and assert
        Assert.True(parent.MatchesFilter("child", a => a.Application.ApplicationName, out _));
        Assert.True(child.MatchesFilter("child", a => a.Application.ApplicationName, out _));
    }

    [Fact]
    public void MatchesFilter_ChildSpanIncludedWhenParentMatched()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);
        var parentSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "parent", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc));
        var childSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "child", parentSpanId: "parent", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc));
        trace.AddSpan(parentSpan);
        trace.AddSpan(childSpan);

        var vms = SpanWaterfallViewModel.Create(trace, [], new SpanWaterfallViewModel.TraceDetailState([], []));
        var parent = vms[0];
        var child = vms[1];

        // Act and assert
        Assert.True(parent.MatchesFilter("parent", a => a.Application.ApplicationName, out var descendents));
        Assert.Equal("child", Assert.Single(descendents).Span.SpanId);
        Assert.False(child.MatchesFilter("parent", a => a.Application.ApplicationName, out _));
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
