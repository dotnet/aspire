// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Tests;
using Aspire.Dashboard.Tests.Shared;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Tests.Shared;

internal static class StructuredLogsSetupHelpers
{
    public static void SetupStructuredLogsDetails(TestContext context)
    {
        context.Services.AddLocalization();
        context.Services.AddSingleton<IInstrumentUnitResolver, TestInstrumentUnitResolver>();
        context.Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        context.Services.AddSingleton<TelemetryRepository>();
        context.Services.AddSingleton<IconResolver>();
        context.Services.AddSingleton<IDialogService, DialogService>();
        context.Services.AddSingleton<LibraryConfiguration>();
        context.Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        context.Services.AddSingleton<DashboardTelemetryService>();
        context.Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        context.Services.AddSingleton<ComponentTelemetryContextProvider>();
        context.Services.AddSingleton<IAIContextProvider, TestAIContextProvider>();
        context.Services.AddSingleton<PauseManager>();
        context.Services.AddSingleton<IDashboardClient>(new TestDashboardClient());

        FluentUISetupHelpers.SetupFluentDivider(context);
        FluentUISetupHelpers.SetupFluentSearch(context);
        FluentUISetupHelpers.SetupFluentDataGrid(context);
        FluentUISetupHelpers.SetupFluentKeyCode(context);
        FluentUISetupHelpers.SetupFluentMenu(context);
        FluentUISetupHelpers.SetupFluentToolbar(context);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(context);
    }
}
