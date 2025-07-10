// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class OtlpSpanTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AllProperties()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", uninstrumentedPeer: false, context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = TelemetryTestHelpers.CreateOtlpScope(context);

        var span = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "abc", parentSpanId: null, startDate: s_testTime,
            statusCode: OtlpSpanStatusCode.Ok, statusMessage: "Status message!", attributes: [new KeyValuePair<string, string>(KnownTraceFields.StatusMessageField, "value")]);

        // Act
        var properties = span.AllProperties();

        // Assert
        Assert.Collection(properties,
            a =>
            {
                Assert.Equal("trace.spanid", a.Key);
                Assert.Equal("abc", a.Value);
            },
            a =>
            {
                Assert.Equal("trace.name", a.Key);
                Assert.Equal("Test", a.Value);
            },
            a =>
            {
                Assert.Equal("trace.kind", a.Key);
                Assert.Equal("Unspecified", a.Value);
            },
            a =>
            {
                Assert.Equal("trace.status", a.Key);
                Assert.Equal("Ok", a.Value);
            },
            a =>
            {
                Assert.Equal("trace.statusmessage", a.Key);
                Assert.Equal("Status message!", a.Value);
            },
            a =>
            {
                Assert.Equal("unknown-trace.statusmessage", a.Key);
                Assert.Equal("value", a.Value);
            });
    }
}
