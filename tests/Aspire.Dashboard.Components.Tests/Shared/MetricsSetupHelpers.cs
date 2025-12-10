// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Tests;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Tests.Shared;

internal static class MetricsSetupHelpers
{
    public static void SetupChartContainer(TestContext context)
    {
        _ = context.JSInterop.SetupModule("/Components/Controls/Chart/MetricTable.razor.js");

        FluentUISetupHelpers.SetupFluentTab(context);
        FluentUISetupHelpers.SetupFluentOverflow(context);

        SetupPlotlyChart(context);
    }

    internal static void SetupPlotlyChart(TestContext context)
    {
        var module = context.JSInterop.SetupModule("/js/app-metrics.js");
        module.SetupVoid("initializeChart", _ => true);
        module.SetupVoid("updateChart", _ => true);

        context.Services.AddLocalization();
        context.Services.AddSingleton<IInstrumentUnitResolver, TestInstrumentUnitResolver>();
        context.Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        context.Services.AddSingleton<PauseManager>();
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<IDialogService, DialogService>();
    }

    internal static void SetupMetricsPage(TestContext context, ISessionStorage? sessionStorage = null)
    {
        FluentUISetupHelpers.SetupFluentDivider(context);
        FluentUISetupHelpers.SetupFluentInputLabel(context);
        FluentUISetupHelpers.SetupFluentDataGrid(context);
        FluentUISetupHelpers.SetupFluentList(context);
        FluentUISetupHelpers.SetupFluentSearch(context);
        FluentUISetupHelpers.SetupFluentKeyCode(context);
        FluentUISetupHelpers.SetupFluentTab(context);
        FluentUISetupHelpers.SetupFluentOverflow(context);
        FluentUISetupHelpers.SetupFluentMenu(context);
        FluentUISetupHelpers.SetupFluentToolbar(context);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(context);

        SetupChartContainer(context);

        context.Services.AddLocalization();
        context.Services.AddSingleton<PauseManager>();
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<IMessageService, MessageService>();
        context.Services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(new DashboardOptions()));
        context.Services.AddSingleton<DimensionManager>();
        context.Services.AddSingleton<IDialogService, DialogService>();
        context.Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        context.Services.AddSingleton<ISessionStorage>(sessionStorage ?? new TestSessionStorage());
        context.Services.AddSingleton<ILocalStorage, TestLocalStorage>();
        context.Services.AddSingleton<ShortcutManager>();
        context.Services.AddSingleton<LibraryConfiguration>();
        context.Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        context.Services.AddSingleton<IThemeResolver, TestThemeResolver>();
        context.Services.AddSingleton<ThemeManager>();
        context.Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        context.Services.AddSingleton<DashboardTelemetryService>();
        context.Services.AddSingleton<ComponentTelemetryContextProvider>();
    }
}
