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
        context.Services.AddLocalization();
        context.Services.AddSingleton<IInstrumentUnitResolver, TestInstrumentUnitResolver>();
        context.Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<IDialogService, DialogService>();
        context.Services.AddSingleton<LibraryConfiguration>();
        context.Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        context.Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        context.Services.AddSingleton<DashboardTelemetryService>();
        context.Services.AddSingleton<ComponentTelemetryContextProvider>();
        context.Services.AddSingleton<IAIContextProvider, TestAIContextProvider>();
        context.Services.AddSingleton<GlobalState>();

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

        context.Services.AddLocalization();
        context.Services.AddSingleton<IAIContextProvider, TestAIContextProvider>();
        context.Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        context.Services.AddSingleton<IconResolver>();
        context.Services.AddSingleton<PauseManager>();
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<IMessageService, MessageService>();
        context.Services.AddSingleton(Options.Create(new DashboardOptions()));
        context.Services.AddSingleton<DimensionManager>();
        context.Services.AddSingleton<ILogger<StructuredLogs>>(NullLogger<StructuredLogs>.Instance);
        context.Services.AddSingleton<IDialogService, DialogService>();
        context.Services.AddSingleton<StructuredLogsViewModel>();
        context.Services.AddSingleton<ISessionStorage, TestSessionStorage>();
        context.Services.AddSingleton<ILocalStorage, TestLocalStorage>();
        context.Services.AddSingleton<ShortcutManager>();
        context.Services.AddSingleton<LibraryConfiguration>();
        context.Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        context.Services.AddFluentUIComponents();
        context.Services.AddScoped<DashboardCommandExecutor, DashboardCommandExecutor>();
        context.Services.AddSingleton<IDashboardClient>(dashboardClient ?? new TestDashboardClient(isEnabled: true, initialResources: [], resourceChannelProvider: Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>));
        context.Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        context.Services.AddSingleton<DashboardTelemetryService>();
        context.Services.AddSingleton<ComponentTelemetryContextProvider>();

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
