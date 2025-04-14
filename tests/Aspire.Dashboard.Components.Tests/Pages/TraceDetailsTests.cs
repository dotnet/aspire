// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Bunit;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class TraceDetailsTests : DashboardTestContext
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Render_HasTrace_SubscriptionRemovedOnDispose()
    {
        // Arrange
        SetupTraceDetailsServices();

        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        var telemetryRepository = Services.GetRequiredService<TelemetryRepository>();
        telemetryRepository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });

        // Act
        var traceId = Convert.ToHexString(Encoding.UTF8.GetBytes("1"));
        var cut = RenderComponent<TraceDetail>(builder =>
        {
            builder.Add(p => p.TraceId, traceId);
            builder.AddCascadingValue(viewport);
        });

        // Assert
        Assert.Collection(telemetryRepository.TracesSubscriptions, t =>
        {
            Assert.Equal(nameof(TelemetryRepository.OnNewTraces), t.Name);
        });

        DisposeComponents();

        Assert.Empty(telemetryRepository.TracesSubscriptions);
    }

    [Fact]
    public async Task Render_ChangeTrace_RowsRendered()
    {
        // Arrange
        SetupTraceDetailsServices();

        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        var telemetryRepository = Services.GetRequiredService<TelemetryRepository>();
        telemetryRepository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(6), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        // Act
        var traceId = Convert.ToHexString(Encoding.UTF8.GetBytes("1"));
        var cut = RenderComponent<TraceDetail>(builder =>
        {
            builder.Add(p => p.TraceId, traceId);
            builder.AddCascadingValue(viewport);
        });

        // Assert
        var grid = cut.FindComponent<FluentDataGrid<SpanWaterfallViewModel>>();
        var rows = grid.FindAll(".fluent-data-grid-row", enableAutoRefresh: true);

        await AsyncTestHelpers.AssertIsTrueRetryAsync(() => rows.Count == 3, "Expected rows to be rendered.");

        traceId = Convert.ToHexString(Encoding.UTF8.GetBytes("2"));
        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.TraceId, traceId);
        });

        await AsyncTestHelpers.AssertIsTrueRetryAsync(() => rows.Count == 2, "Expected rows to be rendered.");
    }

    [Fact]
    public async Task Render_SpansOrderedByStartTime_RowsRenderedInCorrectOrder()
    {
        // Arrange
        SetupTraceDetailsServices();

        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        var telemetryRepository = Services.GetRequiredService<TelemetryRepository>();
        telemetryRepository.AddTraces(new AddContext(),
            new RepeatedField<ResourceSpans>
            {
                new ResourceSpans
                {
                    Resource = CreateResource(),
                    ScopeSpans =
                    {
                        new ScopeSpans
                        {
                            Scope = CreateScope(),
                            Spans =
                            {
                                CreateSpan(traceId: "1", spanId: "1-1",
                                    startTime: s_testTime.AddMinutes(1),
                                    endTime: s_testTime.AddMinutes(10)),
                                CreateSpan(traceId: "1", spanId: "2-1",
                                    startTime: s_testTime.AddMinutes(1),
                                    endTime: s_testTime.AddMinutes(10),
                                    parentSpanId: "1-1"),
                                CreateSpan(traceId: "1", spanId: "3-1",
                                    startTime: s_testTime.AddMinutes(1),
                                    endTime: s_testTime.AddMinutes(10),
                                    parentSpanId: "2-1"),
                                CreateSpan(traceId: "1", spanId: "3-3",
                                    startTime: s_testTime.AddMinutes(3),
                                    endTime: s_testTime.AddMinutes(5),
                                    parentSpanId: "2-1"),
                                CreateSpan(traceId: "1", spanId: "3-2",
                                    startTime: s_testTime.AddMinutes(2),
                                    endTime: s_testTime.AddMinutes(6),
                                    parentSpanId: "2-1")
                            }
                        }
                    }
                }
            });

        // Act
        var traceId = Convert.ToHexString(Encoding.UTF8.GetBytes("1"));
        var cut = RenderComponent<TraceDetail>(builder =>
        {
            builder.Add(p => p.TraceId, traceId);
            builder.AddCascadingValue(viewport);
        });

        var data = await cut.Instance.GetData(new GridItemsProviderRequest<SpanWaterfallViewModel>());

        // Assert
        Assert.Collection(data.Items,
            item => Assert.Equal("Test span. Id: 1-1", item.Span.Name),
            item => Assert.Equal("Test span. Id: 2-1", item.Span.Name),
            item => Assert.Equal("Test span. Id: 3-1", item.Span.Name),
            item => Assert.Equal("Test span. Id: 3-2", item.Span.Name),
            item => Assert.Equal("Test span. Id: 3-3", item.Span.Name));
    }

    [Fact]
    public void ToggleCollapse_SpanStateChanges()
    {
        // Arrange
        SetupTraceDetailsServices();

        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        var telemetryRepository = Services.GetRequiredService<TelemetryRepository>();
        telemetryRepository.AddTraces(new AddContext(),
            new RepeatedField<ResourceSpans>
            {
                new ResourceSpans
                {
                    Resource = CreateResource(),
                    ScopeSpans =
                    {
                        new ScopeSpans
                        {
                            Scope = CreateScope(),
                            Spans =
                            {
                                CreateSpan(traceId: "1", spanId: "1-1",
                                    startTime: s_testTime.AddMinutes(1),
                                    endTime: s_testTime.AddMinutes(10)),
                                CreateSpan(traceId: "1", spanId: "2-1",
                                    startTime: s_testTime.AddMinutes(5),
                                    endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                                CreateSpan(traceId: "1", spanId: "3-1",
                                    startTime: s_testTime.AddMinutes(6),
                                    endTime: s_testTime.AddMinutes(10), parentSpanId: "2-1")
                            }
                        }
                    }
                }
            });

        var traceId = Convert.ToHexString(Encoding.UTF8.GetBytes("1"));
        var cut = RenderComponent<TraceDetail>(builder =>
        {
            builder.Add(p => p.TraceId, traceId);
            builder.AddCascadingValue(viewport);
        });

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".main-grid-expand-button").Count));
        // Act and assert

        // Collapse the middle span
        cut.FindAll(".main-grid-expand-button")[1].Click();

        cut.WaitForAssertion(() =>
        {
            var expandContainers = cut.FindAll(".main-grid-expand-container");
            // There should now be two containers since the 3rd level element should now be filtered out
            Assert.Collection(expandContainers,
                container => Assert.True(container.ClassList.Contains("main-grid-expanded")),
                container => Assert.True(container.ClassList.Contains("main-grid-collapsed")));
        });

        // Collapse the parent span
        cut.FindAll(".main-grid-expand-button")[0].Click();
        cut.WaitForAssertion(() =>
        {
            var expandContainers = cut.FindAll(".main-grid-expand-container");
            // There should now be one container since the 2nd level element should now be filtered out
            Assert.Collection(expandContainers,
                container => Assert.True(container.ClassList.Contains("main-grid-collapsed")));
        });

        // Expand the parent span, we should now see the same two containers as before
        cut.FindAll(".main-grid-expand-button")[0].Click();
        cut.WaitForAssertion(() =>
        {
            var expandContainers = cut.FindAll(".main-grid-expand-container");
            // There should now be two containers since the 3rd level element should now be filtered out
            Assert.Collection(expandContainers,
                container => Assert.True(container.ClassList.Contains("main-grid-expanded")),
                container => Assert.True(container.ClassList.Contains("main-grid-collapsed")));
        });
    }

    private void SetupTraceDetailsServices()
    {
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var dividerModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");

        var inputLabelModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js", version));
        inputLabelModule.SetupVoid("setInputAriaLabel", _ => true);

        var dataGridModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DataGrid/FluentDataGrid.razor.js", version));
        var gridReference = dataGridModule.SetupModule("init", _ => true);
        gridReference.SetupVoid("stop", _ => true);

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Anchor/FluentAnchor.razor.js", version));

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/ListComponentBase.razor.js", version));

        var searchModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);

        var keycodeModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/KeyCode/FluentKeyCode.razor.js", version));
        keycodeModule.Setup<string>("RegisterKeyCode", _ => true);

        var toolbarModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Toolbar/FluentToolbar.razor.js", version));
        toolbarModule.SetupVoid("removePreventArrowKeyNavigation", _ => true);

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Menu/FluentMenu.razor.js", version));

        JSInterop.SetupVoid("initializeContinuousScroll");

        Services.AddLocalization();
        Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        Services.AddSingleton<PauseManager>();
        Services.AddSingleton<TelemetryRepository>();
        Services.AddSingleton<IMessageService, MessageService>();
        Services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(new DashboardOptions()));
        Services.AddSingleton<DimensionManager>();
        Services.AddSingleton<ILogger<StructuredLogs>>(NullLogger<StructuredLogs>.Instance);
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<ISessionStorage, TestSessionStorage>();
        Services.AddSingleton<ILocalStorage, TestLocalStorage>();
        Services.AddSingleton<ShortcutManager>();
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddSingleton<IKeyCodeService, KeyCodeService>();
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
