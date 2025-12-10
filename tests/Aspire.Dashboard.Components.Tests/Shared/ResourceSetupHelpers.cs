// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Tests;
using Aspire.Dashboard.Tests.Shared;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Tests.Shared;

internal static class ResourceSetupHelpers
{
    public static void SetupResourceDetails(TestContext context)
    {
        FluentUISetupHelpers.AddCommonDashboardServices(context);
        context.Services.AddSingleton<IInstrumentUnitResolver, TestInstrumentUnitResolver>();

        FluentUISetupHelpers.SetupFluentDivider(context);
        FluentUISetupHelpers.SetupFluentSearch(context);
        FluentUISetupHelpers.SetupFluentAnchor(context);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(context);
        FluentUISetupHelpers.SetupFluentDataGrid(context);
        FluentUISetupHelpers.SetupFluentKeyCode(context);
        FluentUISetupHelpers.SetupFluentToolbar(context);
        FluentUISetupHelpers.SetupFluentMenu(context);

        context.JSInterop.SetupVoid("scrollToTop", _ => true);
    }

    public static void SetupResourcesPage(TestContext context, ViewportInformation viewport, IDashboardClient? dashboardClient = null)
    {
        FluentUISetupHelpers.SetupFluentDivider(context);
        FluentUISetupHelpers.SetupFluentInputLabel(context);

        var version = typeof(FluentMain).Assembly.GetName().Version!;
        var dataGridModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DataGrid/FluentDataGrid.razor.js", version));
        dataGridModule.SetupModule("init", _ => true);
        dataGridModule.SetupVoid("enableColumnResizing", _ => true);

        FluentUISetupHelpers.SetupFluentSearch(context);
        FluentUISetupHelpers.SetupFluentKeyCode(context);
        FluentUISetupHelpers.SetupFluentCheckbox(context);

        var anchoredRegionModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/AnchoredRegion/FluentAnchoredRegion.razor.js", version));
        anchoredRegionModule.SetupVoid("goToNextFocusableElement", _ => true);

        FluentUISetupHelpers.SetupFluentToolbar(context);
        FluentUISetupHelpers.SetupFluentTab(context);
        FluentUISetupHelpers.SetupFluentOverflow(context);
        FluentUISetupHelpers.SetupFluentMenu(context);

        FluentUISetupHelpers.AddCommonDashboardServices(context);
        context.Services.AddSingleton<IconResolver>();
        context.Services.AddSingleton(Options.Create(new DashboardOptions()));
        context.Services.AddSingleton<DimensionManager>();
        context.Services.AddSingleton<ILogger<StructuredLogs>>(NullLogger<StructuredLogs>.Instance);
        context.Services.AddSingleton<StructuredLogsViewModel>();
        FluentUISetupHelpers.SetupFluentUIComponents(context);
        context.Services.AddScoped<DashboardCommandExecutor, DashboardCommandExecutor>();
        context.Services.AddSingleton<IDashboardClient>(dashboardClient ?? new TestDashboardClient(isEnabled: true, initialResources: [], resourceChannelProvider: Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>));

        var dimensionManager = context.Services.GetRequiredService<DimensionManager>();
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        // Setting a provider ID on menu service is required to simulate <FluentMenuProvider> on the page.
        // This makes FluentMenu render without error.
        var menuService = context.Services.GetRequiredService<IMenuService>();
        menuService.ProviderId = "Test";
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
