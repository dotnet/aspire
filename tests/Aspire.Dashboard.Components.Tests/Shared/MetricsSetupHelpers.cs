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
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        _ = context.JSInterop.SetupModule("/Components/Controls/Chart/MetricTable.razor.js");

        var tabModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Tabs/FluentTab.razor.js", version));
        tabModule.SetupVoid("TabEditable_Changed", _ => true);

        var overflowModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Overflow/FluentOverflow.razor.js", version));
        overflowModule.SetupVoid("fluentOverflowInitialize", _ => true);

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
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var dividerModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");

        var inputLabelModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js", version));
        inputLabelModule.SetupVoid("setInputAriaLabel", _ => true);

        var dataGridModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DataGrid/FluentDataGrid.razor.js", version));
        var dataGridRef = dataGridModule.SetupModule("init", _ => true);
        dataGridRef.SetupVoid("stop");

        context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/ListComponentBase.razor.js", version));

        var searchModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);

        var keycodeModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/KeyCode/FluentKeyCode.razor.js", version));
        keycodeModule.Setup<string>("RegisterKeyCode", _ => true);

        var tabModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Tabs/FluentTab.razor.js", version));
        tabModule.SetupVoid("TabEditable_Changed", _ => true);

        var overflowModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Overflow/FluentOverflow.razor.js", version));
        overflowModule.SetupVoid("fluentOverflowInitialize", _ => true);

        context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Toolbar/FluentToolbar.razor.js", version));

        context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Menu/FluentMenu.razor.js", version));

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

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
