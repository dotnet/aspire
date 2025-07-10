// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Tests;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class LoginTests : DashboardTestContext
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LoginTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Initialize_LongRunningAuthStateFunc_EditContextSet()
    {
        // Arrange
        SetupLoginServices();

        // This represents a long running auth state task.
        var tcs = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        var cut = RenderComponent<Components.Pages.Login>(builder =>
        {
            builder.Add(p => p.AuthenticationState, tcs.Task);
        });

        var instance = cut.Instance;
        var logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        cut.WaitForState(() => instance.EditContext != null);
    }

    private void SetupLoginServices()
    {
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        JSInterop.SetupModule("/Components/Pages/Login.razor.js");

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Anchor/FluentAnchor.razor.js", version));

        var textboxModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/TextField/FluentTextField.razor.js", version));
        textboxModule.SetupVoid("setControlAttribute", _ => true);

        var loggerFactory = IntegrationTestHelpers.CreateLoggerFactory(_testOutputHelper);

        Services.AddLocalization();
        Services.AddSingleton<ILoggerFactory>(loggerFactory);
        Services.AddSingleton<IDashboardClient>(new TestDashboardClient());
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        Services.AddSingleton<DashboardTelemetryService>();
        Services.AddSingleton<ComponentTelemetryContextProvider>();
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
