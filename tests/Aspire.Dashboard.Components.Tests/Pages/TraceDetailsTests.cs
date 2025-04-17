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
using Aspire.Dashboard.Telemetry;
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

    private readonly ITestOutputHelper _testOutputHelper;

    public TraceDetailsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

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
        var loggerFactory = IntegrationTestHelpers.CreateLoggerFactory(_testOutputHelper);
        var logger = loggerFactory.CreateLogger(nameof(Render_ChangeTrace_RowsRendered));

        SetupTraceDetailsServices(loggerFactory: loggerFactory);

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
        logger.LogInformation($"Assert row count for '{traceId}'");
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() =>
        {
            var grid = cut.FindComponent<FluentDataGrid<SpanWaterfallViewModel>>();
            var rows = grid.FindAll(".fluent-data-grid-row");
            return rows.Count == 3;
        }, "Expected rows to be rendered.", logger);

        traceId = Convert.ToHexString(Encoding.UTF8.GetBytes("2"));
        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.TraceId, traceId);
        });

        logger.LogInformation($"Assert row count for '{traceId}'");
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() =>
        {
            var grid = cut.FindComponent<FluentDataGrid<SpanWaterfallViewModel>>();
            var rows = grid.FindAll(".fluent-data-grid-row");
            return rows.Count == 2;
        }, "Expected rows to be rendered.", logger);
    }

    private void SetupTraceDetailsServices(ILoggerFactory? loggerFactory = null)
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
        JSInterop.Setup<string>("getUserAgent").SetResult("TestBrowser");

        loggerFactory ??= NullLoggerFactory.Instance;

        Services.AddLocalization();
        Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        Services.AddSingleton<PauseManager>();
        Services.AddSingleton<TelemetryRepository>();
        Services.AddSingleton<IMessageService, MessageService>();
        Services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(new DashboardOptions()));
        Services.AddSingleton<DimensionManager>();
        Services.AddSingleton<ILoggerFactory>(loggerFactory);
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<ISessionStorage, TestSessionStorage>();
        Services.AddSingleton<ILocalStorage, TestLocalStorage>();
        Services.AddSingleton<ShortcutManager>();
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        Services.AddSingleton<DashboardTelemetryService>();
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
