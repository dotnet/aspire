// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Threading.Channels;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.BrowserStorage;
using Bunit;
using Google.Protobuf.WellKnownTypes;
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

        var testResource = CreateResourceViewModel("test-resource", KnownResourceState.Running);
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
    public void ResourceName_ViaUrlAndResourceLoaded_LogViewerUpdated()
    {
        // Arrange
        var testResource = CreateResourceViewModel("test-resource", KnownResourceState.Running);
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

        cut.WaitForAssertion(() => Assert.Single(subscribedResourceNames));

        logger.LogInformation("Log results are added to log viewer.");
        consoleLogsChannel.Writer.TryWrite([new ResourceLogLine(1, "Hello world", IsErrorMessage: false)]);
        cut.WaitForState(() => instance.LogViewer.LogEntries.EntriesCount > 0);
    }

    private void SetupConsoleLogsServices(TestDashboardClient? dashboardClient = null)
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

        JSInterop.SetupVoid("initializeContinuousScroll");
        JSInterop.SetupVoid("resetContinuousScrollPosition");

        var loggerFactory = IntegrationTestHelpers.CreateLoggerFactory(_testOutputHelper);

        Services.AddLocalization();
        Services.AddSingleton<ILoggerFactory>(loggerFactory);
        Services.AddSingleton<BrowserTimeProvider, TestTimeProvider>();
        Services.AddSingleton<IMessageService, MessageService>();
        Services.AddSingleton<IOptions<DashboardOptions>>(Options.Create(new DashboardOptions()));
        Services.AddSingleton<DimensionManager>();
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<ISessionStorage, TestSessionStorage>();
        Services.AddSingleton<ILocalStorage, TestLocalStorage>();
        Services.AddSingleton<ShortcutManager>();
        Services.AddSingleton<LibraryConfiguration>();
        Services.AddSingleton<IKeyCodeService, KeyCodeService>();
        Services.AddSingleton<IDashboardClient>(dashboardClient ?? new TestDashboardClient());
    }

    private static string GetFluentFile(string filePath, Version version)
    {
        return $"{filePath}?v={version}";
    }

    // display name will be replica set when there are multiple resources with the same display name
    private static ResourceViewModel CreateResourceViewModel(string appName, KnownResourceState? state, string? displayName = null)
    {
        return new ResourceViewModel
        {
            Name = appName,
            ResourceType = "CustomResource",
            DisplayName = displayName ?? appName,
            Uid = Guid.NewGuid().ToString(),
            CreationTimeStamp = DateTime.UtcNow,
            Environment = [],
            Properties = FrozenDictionary<string, Value>.Empty,
            Urls = [],
            Volumes = [],
            State = state?.ToString(),
            KnownState = state,
            StateStyle = null,
            Commands = []
        };
    }
}
