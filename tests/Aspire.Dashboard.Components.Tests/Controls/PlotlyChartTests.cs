// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Bunit;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Controls;

[UseCulture("en-US")]
public class PlotlyChartTests : TestContext
{
    private const string ContainerHtml = "<div id=\"plotly-chart-container\" style=\"width:650px; height:450px;\"></div>";

    [Fact]
    public void Render_NoInstrument_NoPlotlyInvocations()
    {
        // Arrange
        MetricsSetupHelpers.SetupPlotlyChart(this);

        var model = new InstrumentViewModel();

        // Act
        var cut = RenderComponent<PlotlyChart>(builder =>
        {
            builder.Add(p => p.InstrumentViewModel, model);
        });

        // Assert
        cut.MarkupMatches(ContainerHtml);

        Assert.Collection(JSInterop.Invocations,
            i =>
            {
                Assert.Equal("import", i.Identifier);
                Assert.Equal("/js/app-metrics.js", i.Arguments[0]);
            });
    }

    [Fact]
    public async Task Render_HasInstrument_InitializeChartInvocation()
    {
        // Arrange
        MetricsSetupHelpers.SetupPlotlyChart(this);

        var options = new TelemetryLimitOptions();
        var instrument = new OtlpInstrument
        {
            Name = "Name-<b>Bold</b>",
            Unit = "Unit-<b>Bold</b>",
            Options = options,
            Description = "Description-<b>Bold</b>",
            Parent = new OtlpMeter(new InstrumentationScope
            {
                Name = "Parent-Name-<b>Bold</b>"
            }, options),
            Type = OtlpInstrumentType.Sum
        };

        var model = new InstrumentViewModel();
        var dimension = new DimensionScope(capacity: 100, []);
        dimension.AddPointValue(new NumberDataPoint
        {
            AsInt = 1,
            StartTimeUnixNano = 0,
            TimeUnixNano = long.MaxValue
        }, options);

        await model.UpdateDataAsync(instrument, new List<DimensionScope>
        {
            dimension
        });

        // Act
        var cut = RenderComponent<PlotlyChart>(builder =>
        {
            builder.Add(p => p.InstrumentViewModel, model);
            builder.Add(p => p.Duration, TimeSpan.FromSeconds(1));
        });

        // Assert
        cut.MarkupMatches(ContainerHtml);

        Assert.Collection(JSInterop.Invocations,
            i =>
            {
                Assert.Equal("import", i.Identifier);
                Assert.Equal("/js/app-metrics.js", i.Arguments[0]);
            },
            i =>
            {
                Assert.Equal("initializeChart", i.Identifier);
                Assert.Equal("plotly-chart-container", i.Arguments[0]);
                Assert.Collection((IEnumerable<PlotlyTrace>)i.Arguments[1]!, trace =>
                {
                    Assert.Equal("Unit-&lt;b&gt;Bold&lt;/b&gt;", trace.Name);
                    Assert.Equal("<b>Name-&lt;b&gt;Bold&lt;/b&gt;</b><br />Unit-&lt;b&gt;Bold&lt;/b&gt;: 1<br />Time: 12:59:57 AM", trace.Tooltips[0]);
                });
            });
    }
}
