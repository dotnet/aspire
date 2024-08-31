// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;
using OpenTelemetry.Proto.Common.V1;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class DefaultInstrumentUnitResolverTests
{
    [Theory]
    [InlineData("By/s", "instrument_name", "Bytes Per Second")]
    [InlineData("connection", "instrument_name", "Connections")]
    [InlineData("{connection}", "instrument_name", "Connections")]
    [InlineData("", "instrument_name", "Localized:PlotlyChartValue")]
    [InlineData("", "instrument_name.count", "Localized:PlotlyChartCount")]
    [InlineData("", "instrument_name.length", "Localized:PlotlyChartLength")]
    public void ResolveDisplayedUnit(string unit, string name, string expected)
    {
        // Arrange
        var localizer = new TestStringLocalizer<ControlsStrings>();
        var resolver = new DefaultInstrumentUnitResolver(localizer);

        var otlpInstrumentSummary = new OtlpInstrumentSummary
        {
            Description = "Description!",
            Name = name,
            Parent = new OtlpMeter(new InstrumentationScope { Name = "meter_name" }, new TelemetryLimitOptions()),
            Type = OtlpInstrumentType.Gauge,
            Unit = unit
        };

        // Act
        var result = resolver.ResolveDisplayedUnit(otlpInstrumentSummary, titleCase: true, pluralize: true);

        // Assert
        Assert.Equal(expected, result);
    }

    private sealed class TestStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new LocalizedString(name, $"Localized:{name}");
        public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, $"Localized:{name}");

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
