// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Aspire.Dashboard.Utils;
using Aspire.Hosting.ConsoleLogs;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
public partial class ConsoleLogsTests : DashboardTestContext
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ConsoleLogsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ResourceName_SubscribeOnLoadAndChange_SubscribeConsoleLogsOnce()
    {
        // Arrange
        ILogger logger = null!;
        var subscribedResourceNamesChannel = Channel.CreateUnbounded<string>();
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var testResource = ModelTestHelpers.CreateResource(appName: "test-resource", state: KnownResourceState.Running);
        var testResource2 = ModelTestHelpers.CreateResource(appName: "test-resource2", state: KnownResourceState.Running);
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name =>
            {
                logger.LogInformation($"Requesting logs for: {name}");
                subscribedResourceNamesChannel.Writer.TryWrite(name);
                return consoleLogsChannel;
            },
            resourceChannelProvider: () => resourceChannel,
            initialResources: [testResource, testResource2]);

        SetupConsoleLogsServices(dashboardClient);

        logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(DashboardUrls.ConsoleLogsUrl(resource: "test-resource"));

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
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        // Assert 1
        logger.LogInformation("Waiting for selected resource.");
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsWatchingLogs)]);

        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(1, "Test content", IsErrorMessage: false)]);
        consoleLogsChannel.Writer.Complete();

        logger.LogInformation("Waiting for finish message.");
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsFinishedWatchingLogs)]);

        var subscribedResourceName1 = await subscribedResourceNamesChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.Equal("test-resource", subscribedResourceName1);

        navigationManager.LocationChanged += (sender, e) =>
        {
            var expectedUrl = DashboardUrls.ConsoleLogsUrl(resource: "test-resource2");
            Assert.EndsWith(expectedUrl, e.Location);

            cut.SetParametersAndRender(builder =>
            {
                builder.Add(m => m.ResourceName, "test-resource2");
            });
        };
        consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();

        // Act 2
        logger.LogInformation("Changing resource.");
        var resourceSelect = cut.FindComponent<ResourceSelect>();
        var innerSelect = resourceSelect.Find("fluent-select");
        innerSelect.Change("test-resource2");

        // Assert 2
        logger.LogInformation("Waiting for selected resource.");
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource2);
        cut.WaitForState(() => instance.PageViewModel.Status == loc[nameof(Resources.ConsoleLogs.ConsoleLogsWatchingLogs)]);

        var subscribedResourceName2 = await subscribedResourceNamesChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.Equal("test-resource2", subscribedResourceName2);

        subscribedResourceNamesChannel.Writer.Complete();
        Assert.False(await subscribedResourceNamesChannel.Reader.WaitToReadAsync().DefaultTimeout());
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

        cut.WaitForElement("#clear-menu-all");
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

    [Fact]
    public async Task ExecuteCommand_DelayExecuting_IsExecutingReturnsTrueWhileRunning()
    {
        // Arrange
        var testResource = ModelTestHelpers.CreateResource(
            appName: "test-resource",
            state: KnownResourceState.Running,
            commands: [new CommandViewModel("test-name", CommandViewModelState.Enabled, "test-displayname", "test-displaydescription", confirmationMessage: "", parameter: null, isHighlighted: true, iconName: string.Empty, iconVariant: IconVariant.Regular)]);
        var subscribedResourceNameTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var resourceCommandChannel = Channel.CreateUnbounded<ResourceCommandResponseViewModel>();
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name =>
            {
                subscribedResourceNameTcs.TrySetResult(name);
                return consoleLogsChannel;
            },
            resourceChannelProvider: () => resourceChannel,
            resourceCommandsChannel: resourceCommandChannel,
            initialResources: [testResource]);

        SetupConsoleLogsServices(dashboardClient);

        var dimensionManager = Services.GetRequiredService<DimensionManager>();
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        dimensionManager.InvokeOnViewportInformationChanged(viewport);

        var dashboardCommandExecutor = Services.GetRequiredService<DashboardCommandExecutor>();

        var cut = RenderComponent<Components.Pages.ConsoleLogs>(builder =>
        {
            builder.Add(p => p.ResourceName, "test-resource");
            builder.Add(p => p.ViewportInformation, viewport);
        });

        var instance = cut.Instance;
        var logger = Services.GetRequiredService<ILogger<ConsoleLogsTests>>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();

        AngleSharp.Dom.IElement highlightedCommand = default!;
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);
        cut.WaitForAssertion(() =>
        {
            var highlightedCommands = cut.FindAll(".highlighted-command");
            highlightedCommand = Assert.Single(highlightedCommands);
        });

        // Act
        highlightedCommand.Click();

        // Assert
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() => dashboardCommandExecutor.IsExecuting("test-resource", "test-name"), "Command start executing");

        resourceCommandChannel.Writer.TryWrite(new ResourceCommandResponseViewModel
        {
            Kind = ResourceCommandResponseKind.Succeeded
        });

        await AsyncTestHelpers.AssertIsTrueRetryAsync(() => !dashboardCommandExecutor.IsExecuting("test-resource", "test-name"), "Command finish executing");
    }

    [Fact]
    public void PauseResumeButton_TogglePauseResume_LogsPausedAndResumed()
    {
        // Arrange
        var testResource = ModelTestHelpers.CreateResource(appName: "test-resource", state: KnownResourceState.Running);
        var consoleLogsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        var resourceChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            consoleLogsChannelProvider: name => consoleLogsChannel,
            resourceChannelProvider: () => resourceChannel,
            initialResources: [testResource]);

        SetupConsoleLogsServices(dashboardClient);

        var pauseManager = Services.GetRequiredService<PauseManager>();
        var timeProvider = Services.GetRequiredService<BrowserTimeProvider>();
        var loc = Services.GetRequiredService<IStringLocalizer<Resources.ConsoleLogs>>();
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

        // Assert initial state
        cut.WaitForState(() => instance.PageViewModel.SelectedResource == testResource);

        // Pause logs
        var pauseResumeButton = cut.FindComponent<PauseIncomingDataSwitch>();
        pauseResumeButton.Find("fluent-button").Click();

        // A pause line should be visible
        var pauseConsoleLogLine = cut.WaitForElement(".log-pause");

        // Add a new log while paused and assert that the log viewer shows that 1 log was filtered
        var pauseContent = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffK} Log while paused";

        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(1, pauseContent, IsErrorMessage: false)]);
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(2, pauseContent, IsErrorMessage: false)]);
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(3, pauseContent, IsErrorMessage: false)]);

        cut.WaitForAssertion(() => Assert.Equal(
            string.Format(
                loc[Resources.ConsoleLogs.ConsoleLogsPauseActive],
                FormatHelpers.FormatTimeWithOptionalDate(timeProvider,
                    cut.Instance._logEntries.GetEntries().Last().Pause!.StartTime, MillisecondsDisplay.Truncated),
                3),
            pauseConsoleLogLine.TextContent));

        // Resume and write a new log, check that
        // - the pause line has been replaced with pause details
        // - the log viewer shows the new log
        // - the log viewer does not show the discarded log
        pauseResumeButton.Find("fluent-button").Click();
        cut.WaitForAssertion(() => Assert.False(Services.GetRequiredService<PauseManager>().ConsoleLogsPaused));

        var resumeContent = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffK} Log after resume";
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(4, resumeContent, IsErrorMessage: false)]);

        cut.WaitForAssertion(() =>
        {
            var pauseEntry = Assert.Single(cut.Instance._logEntries.GetEntries().Where(e => e.Type is LogEntryType.Pause));
            var pause = pauseEntry.Pause;
            Assert.NotNull(pause);
            Assert.NotNull(pause.EndTime);
            Assert.Equal(
                string.Format(
                    loc[Resources.ConsoleLogs.ConsoleLogsPauseDetails],
                    FormatHelpers.FormatTimeWithOptionalDate(timeProvider, pause.StartTime, MillisecondsDisplay.Truncated),
                    FormatHelpers.FormatTimeWithOptionalDate(timeProvider, pause.EndTime.Value, MillisecondsDisplay.Truncated),
                    3),
                pauseConsoleLogLine.TextContent);
        });

        cut.WaitForAssertion(() =>
        {
            var logViewer = cut.FindComponent<LogViewer>();
            foreach (var logEntry in logViewer.Instance.LogEntries!.GetEntries())
            {
                _testOutputHelper.WriteLine(logEntry.RawContent ?? "no content");
            }
            var newLog = Assert.Single(logViewer.Instance.LogEntries!.GetEntries().Where(e => e.RawContent == resumeContent));
            // We discarded one log while paused, so the new log should be line 3, skipping one
            Assert.Equal(4, newLog.LineNumber);
            Assert.DoesNotContain(pauseContent, logViewer.Instance.LogEntries!.GetEntries().Select(e => e.RawContent));
        });
    }

    private void SetupConsoleLogsServices(TestDashboardClient? dashboardClient = null, TestTimeProvider? timeProvider = null)
    {
        var version = typeof(FluentMain).Assembly.GetName().Version!;

        var dividerModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Divider/FluentDivider.razor.js", version));
        dividerModule.SetupVoid("setDividerAriaOrientation");

        var inputLabelModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js", version));
        inputLabelModule.SetupVoid("setInputAriaLabel", _ => true);

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/ListComponentBase.razor.js", version));

        var searchModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Search/FluentSearch.razor.js", version));
        searchModule.SetupVoid("addAriaHidden", _ => true);

        var keycodeModule = JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/KeyCode/FluentKeyCode.razor.js", version));
        keycodeModule.Setup<string>("RegisterKeyCode", _ => true);

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Menu/FluentMenu.razor.js", version));

        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Anchor/FluentAnchor.razor.js", version));
        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/AnchoredRegion/FluentAnchoredRegion.razor.js", version));
        JSInterop.SetupModule(GetFluentFile("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Toolbar/FluentToolbar.razor.js", version));

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
        Services.AddSingleton<PauseManager>();
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }
}
