// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
