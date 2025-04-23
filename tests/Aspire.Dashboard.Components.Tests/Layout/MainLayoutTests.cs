// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Telemetry;
using Aspire.Dashboard.Utils;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Components.Tooltip;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Layout;

[UseCulture("en-US")]
public partial class MainLayoutTests : DashboardTestContext
{
    [Fact]
    public async Task OnInitialize_UnsecuredOtlp_NotDismissed_DisplayMessageBar()
    {
        // Arrange
        var testLocalStorage = new TestLocalStorage();
        var messageService = new MessageService();

        SetupMainLayoutServices(localStorage: testLocalStorage, messageService: messageService);

        Message? message = null;
        var messageShownTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        messageService.OnMessageItemsUpdatedAsync += () =>
        {
            message = messageService.AllMessages.Single();
            messageShownTcs.TrySetResult();
            return Task.CompletedTask;
        };

        testLocalStorage.OnGetUnprotectedAsync = key =>
        {
            if (key == BrowserStorageKeys.UnsecuredTelemetryMessageDismissedKey)
            {
                return (false, false);
            }
            else
            {
                throw new InvalidOperationException("Unexpected key.");
            }
        };

        var dismissedSettingSetTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        testLocalStorage.OnSetUnprotectedAsync = (key, value) =>
        {
            if (key == BrowserStorageKeys.UnsecuredTelemetryMessageDismissedKey)
            {
                dismissedSettingSetTcs.TrySetResult((bool)value!);
            }
            else
            {
                throw new InvalidOperationException("Unexpected key.");
            }
        };

        // Act
        var cut = RenderComponent<MainLayout>(builder =>
        {
            builder.Add(p => p.ViewportInformation, new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));
        });

        // Assert
        await messageShownTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(message);

        message.Close();

        Assert.True(await dismissedSettingSetTcs.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task OnInitialize_UnsecuredOtlp_Dismissed_NoMessageBar()
    {
        // Arrange
        var testLocalStorage = new TestLocalStorage();
        var messageService = new MessageService();

        SetupMainLayoutServices(localStorage: testLocalStorage, messageService: messageService);

        var messageShownTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        messageService.OnMessageItemsUpdatedAsync += () =>
        {
            messageShownTcs.TrySetResult();
            return Task.CompletedTask;
        };

        testLocalStorage.OnGetUnprotectedAsync = key =>
        {
            if (key == BrowserStorageKeys.UnsecuredTelemetryMessageDismissedKey)
            {
                return (true, true);
            }
            else
            {
                throw new InvalidOperationException("Unexpected key.");
            }
        };

        // Act
        var cut = RenderComponent<MainLayout>(builder =>
        {
            builder.Add(p => p.ViewportInformation, new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));
        });

        // Assert
        var timeoutTask = Task.Delay(100);
        var completedTask = await Task.WhenAny(messageShownTcs.Task, timeoutTask).WaitAsync(TimeSpan.FromSeconds(5));

        // It's hard to test something not happening.
        // In this case of checking for a message, apply a small display and then double check that no message was displayed.
        Assert.True(completedTask != messageShownTcs.Task, "No message bar should be displayed.");
        Assert.Empty(messageService.AllMessages);
    }

    private void SetupMainLayoutServices(TestLocalStorage? localStorage = null, MessageService? messageService = null)
    {
        Services.AddLocalization();
        Services.AddOptions();
        Services.AddSingleton<ThemeManager>();
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<IDashboardClient, TestDashboardClient>();
        Services.AddSingleton<ILocalStorage>(localStorage ?? new TestLocalStorage());
        Services.AddSingleton<IThemeResolver, TestThemeResolver>();
        Services.AddSingleton<ShortcutManager>();
        Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        Services.AddSingleton<IMessageService>(messageService ?? new MessageService());
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddSingleton<ITooltipService, TooltipService>();
        Services.AddSingleton<IToastService, ToastService>();
        Services.AddSingleton<GlobalState>();
        Services.AddSingleton<DashboardTelemetryService>();
        Services.AddSingleton<IDashboardTelemetrySender, TestDashboardTelemetrySender>();
        Services.Configure<DashboardOptions>(o => o.Otlp.AuthMode = OtlpAuthMode.Unsecured);

        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var overflowModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Overflow/FluentOverflow.razor.js", version));
        overflowModule.SetupVoid("fluentOverflowInitialize", _ => true);

        var anchorModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Anchor/FluentAnchor.razor.js", version));

        var themeModule = JSInterop.SetupModule("/js/app-theme.js");

        JSInterop.SetupModule("window.registerGlobalKeydownListener", _ => true);
        JSInterop.SetupModule("window.registerOpenTextVisualizerOnClick", _ => true);

        JSInterop.Setup<BrowserInfo>("window.getBrowserInfo").SetResult(new BrowserInfo { TimeZone = "abc", UserAgent = "mozilla" });
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
