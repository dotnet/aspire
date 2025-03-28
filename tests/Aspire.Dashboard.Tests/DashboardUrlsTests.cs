// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Utils;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class DashboardUrlsTests
{
    private const string PlaceholderInput = "!@#";

    // There is a difference in behavior between Uri.EscapeDataString and QueryHelpers.AddQueryString
    // with relation to ! and @ - they are encoded by the former, but not the latter.
    // It is not required to encode either - some implementations do, and some do not. However, ASP.NET will decode
    // both the encoded and unencoded characters to the same character, so it has no practical effect.
    private const string PlaceholderAllCharactersEncoded = "%21%40%23";
    private const string PlaceholderAllButExclamationMarkEncoded = "!@%23";

    [Fact]
    public void ConsoleLogsUrl_HtmlValues_CorrectlyEscaped()
    {
        Assert.Equal($"/consolelogs/resource/resource{PlaceholderAllCharactersEncoded}", DashboardUrls.ConsoleLogsUrl(resource: $"resource{PlaceholderInput}"));
    }

    [Fact]
    public void StructuredLogsUrl_HtmlValues_CorrectlyEscaped()
    {
        var singleFilterUrl = DashboardUrls.StructuredLogsUrl(
            resource: $"resource{PlaceholderInput}",
            logLevel: "error",
            filters: TelemetryFilterFormatter.SerializeFiltersToString([
                new TelemetryFilter { Condition = FilterCondition.Contains, Field = "test", Value = "value" }
            ]),
            traceId: PlaceholderInput,
            spanId: PlaceholderInput);

        Assert.Equal($"/structuredlogs/resource/resource{PlaceholderAllCharactersEncoded}?logLevel=error&filters=test%3Acontains%3Avalue&traceId={PlaceholderAllButExclamationMarkEncoded}&spanId={PlaceholderAllButExclamationMarkEncoded}", singleFilterUrl);

        var multipleFiltersIncludingSpacesUrl = DashboardUrls.StructuredLogsUrl(
            resource: $"resource{PlaceholderInput}",
            logLevel: "error",
            filters: TelemetryFilterFormatter.SerializeFiltersToString([
                new TelemetryFilter { Condition = FilterCondition.Contains, Field = "test", Value = "value" },
                new TelemetryFilter { Condition = FilterCondition.GreaterThan, Field = "fieldWithSpacedValue", Value = "!! multiple words here !!", Enabled = false },
                new TelemetryFilter { Condition = FilterCondition.NotEqual, Field = "name", Value = "nameValue" },
            ]),
            traceId: PlaceholderInput,
            spanId: PlaceholderInput);
        Assert.Equal($"/structuredlogs/resource/resource{PlaceholderAllCharactersEncoded}?logLevel=error&filters=test%3Acontains%3Avalue%20fieldWithSpacedValue%3Agt%3A!!%2Bmultiple%2Bwords%2Bhere%2B!!%3Adisabled%20name%3A!equals%3AnameValue&traceId={PlaceholderAllButExclamationMarkEncoded}&spanId={PlaceholderAllButExclamationMarkEncoded}", multipleFiltersIncludingSpacesUrl);
    }

    [Fact]
    public void TracesUrl_HtmlValues_CorrectlyEscaped()
    {
        Assert.Equal($"/traces/resource/resource{PlaceholderAllCharactersEncoded}", DashboardUrls.TracesUrl(resource: $"resource{PlaceholderInput}"));
    }

    [Fact]
    public void TraceDetailUrl_HtmlValues_CorrectlyEscaped()
    {
        Assert.Equal($"/traces/detail/traceId{PlaceholderAllCharactersEncoded}", DashboardUrls.TraceDetailUrl(traceId: $"traceId{PlaceholderInput}"));
    }

    [Fact]
    public void MetricsUrl_HtmlValues_CorrectlyEscaped()
    {
        var url = DashboardUrls.MetricsUrl(
            resource: $"resource{PlaceholderInput}",
            meter: $"meter{PlaceholderInput}",
            instrument: $"meter{PlaceholderInput}",
            duration: 10,
            view: "table");

        Assert.Equal($"/metrics/resource/resource{PlaceholderAllCharactersEncoded}?meter=meter{PlaceholderAllButExclamationMarkEncoded}&instrument=meter{PlaceholderAllButExclamationMarkEncoded}&duration=10&view=table", url);
    }

    [Fact]
    public void SetLanguagesUrl_HtmlValues_CorrectlyEscaped()
    {
        Assert.Equal("/api/set-language?language=fr-FR&redirectUrl=%2Fhi", DashboardUrls.SetLanguageUrl("fr-FR", "/hi"));
    }
}
