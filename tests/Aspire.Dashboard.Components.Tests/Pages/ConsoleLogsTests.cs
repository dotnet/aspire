// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class ConsoleLogsTests : TestContext
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ConsoleLogsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ResourceName_MultiRender_SubscribeConsoleLogsOnce()
    {
        // Arrange
        var subscribedResourceNames = new List<string>();
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name =>
            {
                subscribedResourceNames.Add(name);
                return consoleLogsChannel;
            },
            resourceChannelProvider: () => resourceChannel);

        SetupConsoleLogsServices(dashboardClient);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        // Act
        var cut = RenderComponent<Components.Pages.ConsoleLogs>(builder =>
        {
            builder.Add(p => p.ResourceName, "test-resource");
            builder.Add(p => p.ViewportInformation, viewport);
        });

        var instance = cut.Instance;
        var logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        logger.LogInformation("Console log page is waiting for resource.");
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsLoadingResources)]);

        var testResource = ModelTestHelpers.CreateResource(appName: "test-resource", state: KnownResourceState.Running);
        resourceChannel.Writer.TryWrite([
            new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, testResource)
        ]);

        // Assert
        logger.LogInformation("Waiting for selected resource.");
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsWatchingLogs)]);

        // Ensure component is rendered again.
        cut.SetParametersAndRender(builder => { });

        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(1, "Test content", IsErrorMessage: false)]);
        consoleLogsChannel.Writer.Complete();

        logger.LogInformation("Waiting for finish message.");
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)]);

        Assert.Equal("test-resource", Assert.Single(subscribedResourceNames));
    }

    [Fact]
    public async Task ResourceName_ViaUrlAndResourceLoaded_LogViewerUpdated()
    {
        // Arrange
        var testResource = ModelTestHelpers.CreateResource(appName: "test-resource", state: KnownResourceState.Running);
        var subscribedResourceNameTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name =>
            {
                subscribedResourceNameTcs.TrySetResult(name);
                return consoleLogsChannel;
            },
            resourceChannelProvider: () => resourceChannel,
            initialResources: [testResource]);

        SetupConsoleLogsServices(dashboardClient);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        // Act
        var cut = RenderComponent<Components.Pages.ConsoleLogs>(builder =>
        {
            builder.Add(p => p.ResourceName, "test-resource");
            builder.Add(p => p.ViewportInformation, viewport);
        });

        var instance = cut.Instance;
        var logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        // Assert
        logger.LogInformation("Resource and subscription should be set immediately on first render.");
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsWatchingLogs)]);

        var subscribedResource = await subscribedResourceNameTcs.Task;
        Assert.Equal("test-resource", subscribedResource);

        logger.LogInformation("Log results are added to log viewer.");
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(1, "Hello world", IsErrorMessage: false)]);
        cut.WaitForState(() => instance._logEntries.EntriesCount > 0);
    }

    [Fact]
    public void ClearLogEntries_AllResources_LogsFilteredOut()
    {
        // Arrange
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var testResource = ModelTestHelpers.CreateResource(appName: "test-resource", state: KnownResourceState.Running);
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name => consoleLogsChannel,
            resourceChannelProvider: () => resourceChannel,
            initialResources: [testResource]);
        var timeProvider = new TestTimeProvider();

        SetupConsoleLogsServices(dashboardClient, timeProvider: timeProvider);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        // Act
        var cut = RenderComponent<Components.Pages.ConsoleLogs>(builder =>
        {
            builder.Add(p => p.ResourceName, "test-resource");
            builder.Add(p => p.ViewportInformation, viewport);
        });

        var instance = cut.Instance;
        var logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        // Assert
        logger.LogInformation("Waiting for selected resource.");
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsWatchingLogs)]);

        logger.LogInformation("Log results are added to log viewer.");
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(1, "2025-02-08T10:16:08Z Hello world", IsErrorMessage: false)]);
        cut.WaitForState(() => instance._logEntries.EntriesCount > 0);

        // Set current time to the date of the first entry so all entries are cleared.
        var earliestEntry = instance._logEntries.GetEntries()[0];
        timeProvider.UtcNow = earliestEntry.Timestamp!.Value;

        logger.LogInformation("Clear current entries.");
        cut.Find(".clear-button").Click();
        cut.Find("#clear-menu-all").Click();

        cut.WaitForState(() => instance._logEntries.EntriesCount == 0);

        logger.LogInformation("New log results are added to log viewer.");
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(2, "2025-03-08T10:16:08Z Hello world", IsErrorMessage: false)]);
        cut.WaitForState(() => instance._logEntries.EntriesCount > 0);
    }

    [Fact]
    public async Task ConsoleLogsManager_ClearLogs_LogsFilteredOutAsync()
    {
        // Arrange
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var testResource = ModelTestHelpers.CreateResource(appName: "test-resource", state: KnownResourceState.Running);
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name => consoleLogsChannel,
            resourceChannelProvider: () => resourceChannel,
            initialResources: [testResource]);
        var timeProvider = new TestTimeProvider();

        SetupConsoleLogsServices(dashboardClient, timeProvider: timeProvider);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        dimensionManager.InvokeOnViewportInformationChanged(viewport);
        var consoleLogsManager = Services.GetRequiredService<ConsoleLogsManager>();

        // Act
        var cut = RenderComponent<Components.Pages.ConsoleLogs>(builder =>
        {
            builder.Add(p => p.ResourceName, "test-resource");
            builder.Add(p => p.ViewportInformation, viewport);
        });

        var instance = cut.Instance;
        var logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        // Assert
        Assert.Single(consoleLogsManager.GetSubscriptions());

        logger.LogInformation("Waiting for selected resource.");
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsWatchingLogs)]);

        logger.LogInformation("Log results are added to log viewer.");
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(1, "2025-02-08T10:16:08Z Hello world", IsErrorMessage: false)]);
        cut.WaitForState(() => instance._logEntries.EntriesCount > 0);

        // Set current time to the date of the first entry so all entries are cleared.
        var earliestEntry = instance._logEntries.GetEntries()[0];
        timeProvider.UtcNow = earliestEntry.Timestamp!.Value;

        await consoleLogsManager.UpdateFiltersAsync(new ConsoleLogsFilters { FilterAllLogsDate = earliestEntry.Timestamp!.Value });

        cut.WaitForState(() => instance._logEntries.EntriesCount == 0);

        logger.LogInformation("New log results are added to log viewer.");
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(2, "2025-03-08T10:16:08Z Hello world", IsErrorMessage: false)]);
        cut.WaitForState(() => instance._logEntries.EntriesCount > 0);
    }

    [Fact]
    public void MenuButtons_SelectedResourceChanged_ButtonsUpdated()
    {
        // Arrange
        var testResource = ModelTestHelpers.CreateResource(
            appName: "test-resource",
            state: KnownResourceState.Running,
            commands: [new CommandViewModel("test-name", CommandViewModelState.Enabled, "test-displayname", "test-displaydescription", confirmationMessage: "", parameter: null, isHighlighted: true, iconName: string.Empty, iconVariant: IconVariant.Regular)]);
        var subscribedResourceNameTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name =>
            {
                subscribedResourceNameTcs.TrySetResult(name);
                return consoleLogsChannel;
            },
            resourceChannelProvider: () => resourceChannel,
            initialResources: [testResource]);

        SetupConsoleLogsServices(dashboardClient);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        // Act 1
        var cut = RenderComponent<Components.Pages.ConsoleLogs>(builder =>
        {
            builder.Add(p => p.ResourceName, "test-resource");
            builder.Add(p => p.ViewportInformation, viewport);
        });

        var instance = cut.Instance;
        var logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        // Assert 1
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);

        cut.WaitForAssertion(() =>
        {
            var highlightedCommands = cut.FindAll(".highlighted-command");
            Assert.Single(highlightedCommands);
        });

        // Act 2
        testResource = ModelTestHelpers.CreateResource(
            appName: "test-resource",
            state: KnownResourceState.Running,
            commands: []);
        resourceChannel.Writer.TryWrite([
            new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, testResource)
        ]);

        // Assert 2
        cut.WaitForAssertion(() =>
        {
            var highlightedCommands = cut.FindAll(".highlighted-command");
            Assert.Empty(highlightedCommands);
        });
    }

    private void SetupConsoleLogsServices(TestDashboardClient? dashboardClient = null, TestTimeProvider? timeProvider = null)
    {
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var dividerModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");

        var inputLabelModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js", version));
        inputLabelModule.SetupVoid("setInputAriaLabel", _ => true);

        var listModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/ListComponentBase.razor.js", version));

        var searchModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);

        var keycodeModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/KeyCode/FluentKeyCode.razor.js", version));
        keycodeModule.Setup<string>("RegisterKeyCode", _ => true);

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Anchor/FluentAnchor.razor.js", version));
        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/AnchoredRegion/FluentAnchoredRegion.razor.js", version));

        JSInterop.SetupVoid("initializeContinuousScroll");
        JSInterop.SetupVoid("resetContinuousScrollPosition");

        var loggerFactory = IntegrationTestHelpers.CreateLoggerFactory(_testOutputHelper);

        Services.AddLocalization();
        Services.AddSingleton<ILoggerFactory>(loggerFactory);
        Services.AddSingleton<BrowserTimeProvider>(timeProvider ?? new TestTimeProvider());
        Services.AddSingleton<IMessageService, MessageService>();
        Services.AddSingleton<IToastService, ToastService>();
        Services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(new DashboardOptions()));
        Services.AddSingleton<DimensionManager>();
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<ISessionStorage, TestSessionStorage>();
        Services.AddSingleton<ILocalStorage, TestLocalStorage>();
        Services.AddSingleton<ShortcutManager>();
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        Services.AddSingleton<IDashboardClient>(dashboardClient ?? new TestDashboardClient());
        Services.AddSingleton<DashboardCommandExecutor>();
        Services.AddSingleton<ConsoleLogsManager>();
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
