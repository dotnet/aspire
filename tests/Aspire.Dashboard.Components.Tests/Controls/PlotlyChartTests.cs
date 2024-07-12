// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Otlp.Storage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
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
        JSInterop.SetupModule("/js/app-metrics.js");

        Services.AddLocalization();
        Services.AddSingleton<IInstrumentUnitResolver, TestInstrumentUnitResolver>();
        Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        Services.AddSingleton<TelemetryRepository>();
        Services.AddSingleton<IDialogService, DialogService>();

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
        var module = JSInterop.SetupModule("/js/app-metrics.js");
        module.SetupVoid("initializeChart", _ => true);

        Services.AddLocalization();
        Services.AddSingleton<IInstrumentUnitResolver, TestInstrumentUnitResolver>();
        Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        Services.AddSingleton<TelemetryRepository>();
        Services.AddSingleton<IDialogService, DialogService>();

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

    private sealed class TestInstrumentUnitResolver : IInstrumentUnitResolver
    {
        public string ResolveDisplayedUnit(OtlpInstrument instrument, bool titleCase, bool pluralize)
        {
            return instrument.Unit;
        }
    }

    private sealed class TestTimeProvider : BrowserTimeProvider
    {
        private TimeZoneInfo? _localTimeZone;

        public TestTimeProvider() : base(NullLoggerFactory.Instance)
        {
        }

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2025, 12, 20, 23, 59, 59, TimeSpan.Zero);
        }

        public override TimeZoneInfo LocalTimeZone => _localTimeZone ??= TimeZoneInfo.CreateCustomTimeZone(nameof(PlotlyChartTests), TimeSpan.FromHours(1), nameof(PlotlyChartTests), nameof(PlotlyChartTests));
    }
}
