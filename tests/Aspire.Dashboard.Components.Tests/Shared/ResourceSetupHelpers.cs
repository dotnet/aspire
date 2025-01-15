// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
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

        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var dividerModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");

        var searchModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);

        var anchorModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Anchor/FluentAnchor.razor.js", version));

        var anchoredRegionModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/AnchoredRegion/FluentAnchoredRegion.razor.js", version));

        var dataGridModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DataGrid/FluentDataGrid.razor.js", version));
        var dataGridRef = dataGridModule.SetupModule("init", _ => true);
        dataGridRef.SetupVoid("stop");

        var keycodeModule = context.JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/KeyCode/FluentKeyCode.razor.js", version));
        keycodeModule.Setup<string>("RegisterKeyCode", _ => true);

        context.JSInterop.SetupVoid("scrollToTop", _ => true);
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
