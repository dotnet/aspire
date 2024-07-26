// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Controls;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Otlp.Storage;
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

        var tabModule = context.JSInterop.SetupModule("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Tabs/FluentTab.razor.js?v=4.9.3.24205");
        tabModule.SetupVoid("TabEditable_Changed", _ => true);

        var overflowModule = context.JSInterop.SetupModule("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Overflow/FluentOverflow.razor.js?v=4.9.3.24205");
        overflowModule.SetupVoid("fluentOverflowInitialize", _ => true);

        context.Services.AddSingleton<CurrentChartViewModel>();

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
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<IDialogService, DialogService>();
    }

    internal static void SetupMetricsPage(TestContext context)
    {
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var dividerModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");

        var inputLabelModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js", version));
        inputLabelModule.SetupVoid("setInputAriaLabel", _ => true);

        var dataGridModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DataGrid/FluentDataGrid.razor.js", version));
        var dataGridRef = dataGridModule.SetupModule("init", _ => true);
        dataGridRef.SetupVoid("stop");

        var listModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/ListComponentBase.razor.js", version));

        var searchModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);

        MetricsSetupHelpers.SetupChartContainer(context);

        context.Services.AddLocalization();
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<IMessageService, MessageService>();
        context.Services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(new DashboardOptions()));
        context.Services.AddSingleton<DimensionManager>();
        context.Services.AddSingleton<IDialogService, DialogService>();
        context.Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        context.Services.AddSingleton<ISessionStorage, TestSessionStorage>();
        context.Services.AddSingleton<ILocalStorage, TestLocalStorage>();
        context.Services.AddSingleton<ShortcutManager>();
        context.Services.AddSingleton<LibraryConfiguration>();
        context.Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        context.Services.AddSingleton<ThemeManager>();
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
