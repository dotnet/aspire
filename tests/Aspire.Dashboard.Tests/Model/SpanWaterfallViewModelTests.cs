// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class SpanWaterfallViewModelTests
{
    [Fact]
    public void Create_HasChildren_ChildrenPopulated()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var app2 = new OtlpApplication("app2", "instance", context);

        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc)));

        // Act
        var vm = SpanWaterfallViewModel.Create(trace, new SpanWaterfallViewModel.TraceDetailState([], []));

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
        var app = new OtlpApplication("app1", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);

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
            new SpanWaterfallViewModel.TraceDetailState(
                [new TestPeerResolver()],
                []
            )).First();

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
        var app1 = new OtlpApplication("app1", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        var parentSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "parent", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc));
        var childSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "child", parentSpanId: "parent", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc));
        trace.AddSpan(parentSpan);
        trace.AddSpan(childSpan);

        var vms = SpanWaterfallViewModel.Create(trace, new SpanWaterfallViewModel.TraceDetailState([], []));
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
        var app1 = new OtlpApplication("app1", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        var parentSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "parent", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc));
        var childSpan = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "child", parentSpanId: "parent", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc));
        trace.AddSpan(parentSpan);
        trace.AddSpan(childSpan);

        var vms = SpanWaterfallViewModel.Create(trace, new SpanWaterfallViewModel.TraceDetailState([], []));
        var parent = vms[0];
        var child = vms[1];

        // Act and assert
        Assert.True(parent.MatchesFilter("parent", a => a.Application.ApplicationName, out var descendents));
        Assert.Equal("child", Assert.Single(descendents).Span.SpanId);
        Assert.False(child.MatchesFilter("parent", a => a.Application.ApplicationName, out _));
    }

    private sealed class TestPeerResolver : IOutgoingPeerResolver
    {
        public bool TryResolvePeerName(KeyValuePair<string, string>[] attributes, out string? name)
        {
            var peerService = attributes.FirstOrDefault(a => a.Key == "peer.service");
            if (!string.IsNullOrEmpty(peerService.Value))
            {
                name = peerService.Value;
                return true;
            }

            name = null;
            return false;
        }

        public IDisposable OnPeerChanges(Func<Task> callback)
        {
            return EmptyDisposable.Instance;
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
