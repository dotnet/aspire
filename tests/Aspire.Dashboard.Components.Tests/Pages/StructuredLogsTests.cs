// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Storage;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class StructuredLogsTests : TestContext
{
    [Fact]
    public void Render_TraceIdAndSpanId_FilterAdded()
    {
        // Arrange
        SetupStructureLogsServices();

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["TraceId"] = "123",
            ["SpanId"] = "456"
        });
        navigationManager.NavigateTo(uri);

        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        // Act
        var cut = RenderComponent<StructuredLogs>(builder =>
        {
            builder.Add(p => p.ViewportInformation, viewport);
        });

        // Assert
        var viewModel = Services.GetRequiredService<StructuredLogsViewModel>();

        Assert.Collection(viewModel.Filters,
            f =>
            {
                Assert.Equal(KnownStructuredLogFields.TraceIdField, f.Field);
                Assert.Equal("123", f.Value);
            },
            f =>
            {
                Assert.Equal(KnownStructuredLogFields.SpanIdField, f.Field);
                Assert.Equal("456", f.Value);
            });
    }

    [Fact]
    public void Render_DuplicateFilters_SingleFilterAdded()
    {
        // Arrange
        SetupStructureLogsServices();

        var logFilter = new TelemetryFilter { Field = "TestField", Condition = FilterCondition.Contains, Value = "TestValue" };
        var serializedFilter = LogFilterFormatter.SerializeLogFiltersToString([logFilter]);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["filters"] = serializedFilter + "+" + serializedFilter,
        });
        navigationManager.NavigateTo(uri);

        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        // Act
        var cut = RenderComponent<StructuredLogs>(builder =>
        {
            builder.Add(p => p.ViewportInformation, viewport);
        });

        // Assert
        var viewModel = Services.GetRequiredService<StructuredLogsViewModel>();

        Assert.Collection(viewModel.Filters,
            f =>
            {
                Assert.Equal(logFilter.Field, f.Field);
                Assert.Equal(logFilter.Condition, f.Condition);
                Assert.Equal(logFilter.Value, f.Value);
            });
    }

    private void SetupStructureLogsServices()
    {
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var dividerModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");

        var inputLabelModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js", version));
        inputLabelModule.SetupVoid("setInputAriaLabel", _ => true);

        var dataGridModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DataGrid/FluentDataGrid.razor.js", version));
        dataGridModule.SetupModule("init", _ => true);

        var listModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/ListComponentBase.razor.js", version));

        var searchModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);

        var keycodeModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/KeyCode/FluentKeyCode.razor.js", version));
        keycodeModule.Setup<string>("RegisterKeyCode", _ => true);

        JSInterop.SetupVoid("initializeContinuousScroll");

        Services.AddLocalization();
        Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        Services.AddSingleton<TelemetryRepository>();
        Services.AddSingleton<IMessageService, MessageService>();
        Services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(new DashboardOptions()));
        Services.AddSingleton<DimensionManager>();
        Services.AddSingleton<ILogger<StructuredLogs>>(NullLogger<StructuredLogs>.Instance);
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<StructuredLogsViewModel>();
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
