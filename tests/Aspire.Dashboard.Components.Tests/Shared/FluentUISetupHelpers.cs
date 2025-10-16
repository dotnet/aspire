// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Tests;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Tests.Shared;

/// <summary>
/// Provides reusable setup methods for FluentUI components and services in tests.
/// </summary>
internal static class FluentUISetupHelpers
{
    /// <summary>
    /// Gets the FluentUI assembly version used for module paths.
    /// </summary>
    public static Version GetFluentUIVersion() => typeof(FluentMain).Assembly.GetName().Version!;

    /// <summary>
    /// Formats a FluentUI module file path with version query parameter.
    /// </summary>
    public static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }

    /// <summary>
    /// Sets up the FluentDialogProvider JSInterop module.
    /// </summary>
    public static void SetupFluentDialogProvider(TestContext context)
    {
        var version = GetFluentUIVersion();
        var dialogProviderModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Dialog/FluentDialogProvider.razor.js", version));
        dialogProviderModule.SetupModule("getActiveElement", _ => true);
    }

    /// <summary>
    /// Sets up the FluentMenu JSInterop module.
    /// </summary>
    public static void SetupFluentMenu(TestContext context)
    {
        var version = GetFluentUIVersion();
        var menuModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Menu/FluentMenu.razor.js", version));
        menuModule.SetupVoid("initialize", _ => true);
        menuModule.SetupVoid("dispose", _ => true);
    }

    /// <summary>
    /// Sets up the FluentOverflow JSInterop module.
    /// </summary>
    public static void SetupFluentOverflow(TestContext context)
    {
        var version = GetFluentUIVersion();
        var overflowModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Overflow/FluentOverflow.razor.js", version));
        overflowModule.SetupVoid("fluentOverflowInitialize", _ => true);
        overflowModule.SetupVoid("fluentOverflowDispose", _ => true);
    }

    /// <summary>
    /// Sets up the FluentAnchor JSInterop module.
    /// </summary>
    public static void SetupFluentAnchor(TestContext context)
    {
        var version = GetFluentUIVersion();
        context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Anchor/FluentAnchor.razor.js", version));
    }

    /// <summary>
    /// Sets up the FluentAnchoredRegion JSInterop module.
    /// </summary>
    public static void SetupFluentAnchoredRegion(TestContext context)
    {
        var version = GetFluentUIVersion();
        context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/AnchoredRegion/FluentAnchoredRegion.razor.js", version));
    }

    /// <summary>
    /// Sets up the FluentDivider JSInterop module.
    /// </summary>
    public static void SetupFluentDivider(TestContext context)
    {
        var version = GetFluentUIVersion();
        var dividerModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");
    }

    /// <summary>
    /// Sets up the FluentDataGrid JSInterop module.
    /// </summary>
    public static void SetupFluentDataGrid(TestContext context)
    {
        var version = GetFluentUIVersion();
        var dataGridModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DataGrid/FluentDataGrid.razor.js", version));
        var gridReference = dataGridModule.SetupModule("init", _ => true);
        gridReference.SetupVoid("stop", _ => true);
    }

    /// <summary>
    /// Sets up the FluentSearch JSInterop module.
    /// </summary>
    public static void SetupFluentSearch(TestContext context)
    {
        var version = GetFluentUIVersion();
        var searchModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);
    }

    /// <summary>
    /// Sets up the FluentKeyCode JSInterop module.
    /// </summary>
    public static void SetupFluentKeyCode(TestContext context)
    {
        var version = GetFluentUIVersion();
        var keycodeModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/KeyCode/FluentKeyCode.razor.js", version));
        keycodeModule.Setup<string>("RegisterKeyCode", _ => true);
    }

    /// <summary>
    /// Sets up the FluentToolbar JSInterop module.
    /// </summary>
    public static void SetupFluentToolbar(TestContext context)
    {
        var version = GetFluentUIVersion();
        var toolbarModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Toolbar/FluentToolbar.razor.js", version));
        toolbarModule.SetupVoid("removePreventArrowKeyNavigation", _ => true);
    }

    /// <summary>
    /// Sets up the FluentInputLabel JSInterop module.
    /// </summary>
    public static void SetupFluentInputLabel(TestContext context)
    {
        var version = GetFluentUIVersion();
        var inputLabelModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js", version));
        inputLabelModule.SetupVoid("setInputAriaLabel", _ => true);
    }

    /// <summary>
    /// Adds common Aspire Dashboard services to the test context.
    /// </summary>
    public static void AddCommonDashboardServices(
        TestContext context,
        ILocalStorage? localStorage = null,
        ISessionStorage? sessionStorage = null,
        ThemeManager? themeManager = null,
        IMessageService? messageService = null,
        bool includeTelemetryErrorRecorder = false)
    {
        context.Services.AddLocalization();
        context.Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<PauseManager>();
        context.Services.AddSingleton<IDialogService, DialogService>();
        context.Services.AddSingleton<ILocalStorage>(localStorage ?? new TestLocalStorage());
        context.Services.AddSingleton<ISessionStorage>(sessionStorage ?? new TestSessionStorage());
        context.Services.AddSingleton<ShortcutManager>();
        context.Services.AddSingleton<LibraryConfiguration>();
        context.Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        context.Services.AddSingleton<IMessageService>(messageService ?? new MessageService());
        context.Services.AddSingleton<DashboardTelemetryService>();
        context.Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        context.Services.AddSingleton<ComponentTelemetryContextProvider>();
        context.Services.AddSingleton<IAIContextProvider, TestAIContextProvider>();

        if (includeTelemetryErrorRecorder)
        {
            context.Services.AddSingleton<ITelemetryErrorRecorder, TestTelemetryErrorRecorder>();
        }

        if (themeManager is not null)
        {
            context.Services.AddSingleton(themeManager);
        }
        else
        {
            context.Services.AddSingleton(new ThemeManager(new TestThemeResolver()));
        }
    }

    /// <summary>
    /// Adds FluentUI components and sets up the menu service provider.
    /// </summary>
    public static void SetupFluentUIComponents(TestContext context)
    {
        context.Services.AddFluentUIComponents();

        var menuService = context.Services.GetRequiredService<IMenuService>();
        menuService.ProviderId = "Test";
    }

    /// <summary>
    /// Sets up common dialog-related JSInterop modules and services.
    /// </summary>
    public static void SetupDialogInfrastructure(
        TestContext context,
        ThemeManager? themeManager = null,
        ILocalStorage? localStorage = null,
        bool includeTelemetryErrorRecorder = false)
    {
        AddCommonDashboardServices(context, localStorage: localStorage, themeManager: themeManager, includeTelemetryErrorRecorder: includeTelemetryErrorRecorder);
        SetupFluentUIComponents(context);
        SetupFluentDialogProvider(context);
    }

    /// <summary>
    /// Renders a FluentDialogProvider component for dialog testing.
    /// </summary>
    public static IRenderedFragment RenderDialogProvider(TestContext context)
    {
        return context.Render(builder =>
        {
            builder.OpenComponent<FluentDialogProvider>(0);
            builder.CloseComponent();
        });
    }
}
