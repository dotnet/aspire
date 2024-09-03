// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Web;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Bunit;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;
using static Aspire.Dashboard.Components.Pages.Metrics;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class MetricsTests : TestContext
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ChangeResource_MeterAndInstrumentOnNewResource_InstrumentSet()
    {
        ChangeResourceAndAssertInstrument(
            app1InstrumentName: "test1",
            app2InstrumentName: "test1",
            expectedInstrumentNameAfterChange: "test1");
    }

    [Fact]
    public void ChangeResource_MeterAndInstrumentNotOnNewResources_InstrumentCleared()
    {
        ChangeResourceAndAssertInstrument(
            app1InstrumentName: "test1",
            app2InstrumentName: "test2",
            expectedInstrumentNameAfterChange: null);
    }

    [Fact]
    public void InitialLoad_HasSessionState_RedirectUsingState()
    {
        // Arrange
        var testSessionStorage = new TestSessionStorage
        {
            OnGetAsync = key =>
            {
                if (key == BrowserStorageKeys.MetricsPageState)
                {
                    var state = new MetricsPageState
                    {
                        ApplicationName = "TestApp",
                        MeterName = "test-meter",
                        InstrumentName = "test-instrument",
                        DurationMinutes = 720,
                        ViewKind = MetricViewKind.Table.ToString()
                    };
                    return (true, state);
                }
                else
                {
                    throw new InvalidOperationException("Unexpected key: " + key);
                }
            }
        };
        MetricsSetupHelpers.SetupMetricsPage(this, sessionStorage: testSessionStorage);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(DashboardUrls.MetricsUrl());

        Uri? loadRedirect = null;
        navigationManager.LocationChanged += (s, a) =>
        {
            loadRedirect = new Uri(a.Location);
        };

        var telemetryRepository = Services.GetRequiredService<TelemetryRepository>();
        telemetryRepository.AddMetrics(new AddContext(), new RepeatedField<ResourceMetrics>
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "TestApp"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test-instrument", startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act
        var cut = RenderComponent<Metrics>(builder =>
        {
            builder.AddCascadingValue(new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));
        });

        // Assert
        Assert.NotNull(loadRedirect);
        Assert.Equal("/metrics/resource/TestApp", loadRedirect.AbsolutePath);

        var query = HttpUtility.ParseQueryString(loadRedirect.Query);
        Assert.Equal("test-meter", query["meter"]);
        Assert.Equal("test-instrument", query["instrument"]);
        Assert.Equal("720", query["duration"]);
        Assert.Equal(MetricViewKind.Table.ToString(), query["view"]);
    }

    private void ChangeResourceAndAssertInstrument(string app1InstrumentName, string app2InstrumentName, string? expectedInstrumentNameAfterChange)
    {
        // Arrange
        MetricsSetupHelpers.SetupMetricsPage(this);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(DashboardUrls.MetricsUrl(resource: "TestApp", meter: "test-meter", instrument: app1InstrumentName, duration: 720, view: MetricViewKind.Table.ToString()));

        var telemetryRepository = Services.GetRequiredService<TelemetryRepository>();
        telemetryRepository.AddMetrics(new AddContext(), new RepeatedField<ResourceMetrics>
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "TestApp"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: app1InstrumentName, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "TestApp2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: app2InstrumentName, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act 1
        var cut = RenderComponent<Metrics>(builder =>
        {
            builder.Add(m => m.ApplicationName, "TestApp");
            builder.AddCascadingValue(new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));
        });

        var viewModel = cut.Instance.PageViewModel;

        // Assert 1
        Assert.Equal("test-meter", viewModel.SelectedMeter!.MeterName);
        Assert.Equal(app1InstrumentName, viewModel.SelectedInstrument!.Name);

        // Act 2
        var resourceSelect = cut.FindComponent<ResourceSelect>();
        var innerSelect = resourceSelect.Find("fluent-select");
        innerSelect.Change("TestApp2");

        cut.WaitForAssertion(() => Assert.Equal("TestApp2", viewModel.SelectedApplication.Name), TestConstants.WaitTimeout);

        Assert.Equal(expectedInstrumentNameAfterChange, viewModel.SelectedInstrument?.Name);
        if (expectedInstrumentNameAfterChange != null)
        {
            // Meter is cleared if instrument is cleared.
            Assert.Equal("test-meter", viewModel.SelectedMeter!.MeterName);
        }

        Assert.Equal(MetricViewKind.Table, viewModel.SelectedViewKind);
        Assert.Equal(TimeSpan.FromMinutes(720), viewModel.SelectedDuration.Id);
    }
}
