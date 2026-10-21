// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Layout;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Tests.Shared;
using Aspire.Dashboard.Utils;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Layout;

public partial class MainLayoutTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TerminalsShortcut_RequiresLiveResourceService(bool isEnabled, bool isReadOnly)
    {
        var client = new TestDashboardClient(isEnabled: isEnabled,
            initialResources: [TerminalSetupHelpers.CreateTerminalResource("shell")],
            resourceChannelProvider: () => Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>())
        {
            IsReadOnly = isReadOnly
        };
        SetupMainLayoutServices(dashboardClient: client);
        var cut = Render<MainLayout>(builder => builder.Add(p => p.ViewportInformation,
            new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false)));
        var shortcuts = Services.GetRequiredService<ShortcutManager>();
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var initialUri = navigationManager.Uri;
        var expectedAvailable = isEnabled && !isReadOnly;
        cut.WaitForAssertion(() => Assert.Equal(expectedAvailable, shortcuts.IsShortcutAvailable(AspireKeyboardShortcut.GoToTerminals)));

        await cut.InvokeAsync(() => cut.Instance.OnPageKeyDownAsync(AspireKeyboardShortcut.GoToTerminals));

        Assert.Equal(expectedAvailable ? navigationManager.ToAbsoluteUri(DashboardUrls.TerminalsUrl()).AbsoluteUri : initialUri,
            navigationManager.Uri);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task ResourceNavigation_RunSwitchCancelsAndRestartsAvailabilityWatch(bool isDesktop, bool historicalHasParameters)
    {
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialResources = new List<ResourceViewModel>
        {
            TerminalSetupHelpers.CreateTerminalResource("shell"),
            ModelTestHelpers.CreateResource("parameter", resourceType: KnownResourceTypes.Parameter)
        };
        var client = new TestDashboardClient(isEnabled: true,
            initialResources: initialResources,
            resourceChannelProvider: () => Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>())
        {
            OnResourceSubscriptionDisposed = () => disposed.TrySetResult()
        };
        var runStore = new FluentUISetupHelpers.TestDashboardRunStore(
        [
            new("current", DashboardRunStore.SchemaVersion, DateTimeOffset.UnixEpoch, null, false, "TestApp", string.Empty, true),
            new("historical", DashboardRunStore.SchemaVersion, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, true, "TestApp", string.Empty, false)
        ]);
        SetupMainLayoutServices(dashboardRunStore: runStore, dashboardClient: client);
        var selection = Assert.IsType<FluentUISetupHelpers.TestDashboardRunSelection>(Services.GetRequiredService<IDashboardRunSelection>());
        selection.OnSelectRun = runId =>
        {
            client.IsReadOnly = runId is not null;
            initialResources.RemoveAll(resource => resource.IsParameter);
            if (runId is null || historicalHasParameters)
            {
                initialResources.Add(ModelTestHelpers.CreateResource("parameter", resourceType: KnownResourceTypes.Parameter));
            }
        };
        var cut = Render<MainLayout>(builder => builder.Add(p => p.ViewportInformation,
            new ViewportInformation(IsDesktop: isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false)));
        var shortcuts = Services.GetRequiredService<ShortcutManager>();
        cut.WaitForAssertion(() =>
        {
            Assert.True(HasTerminals());
            Assert.True(HasParameters());
            Assert.True(shortcuts.IsShortcutAvailable(AspireKeyboardShortcut.GoToTerminals));
        });
        Assert.Equal(1, client.ResourceSubscriptionCount);
        await cut.InvokeAsync(() => cut.Instance.SwitchDashboardRunAsync("historical"));
        await disposed.Task.WaitAsync(DefaultWaitTimeout);
        Assert.False(HasTerminals());
        Assert.False(shortcuts.IsShortcutAvailable(AspireKeyboardShortcut.GoToTerminals));
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(historicalHasParameters, HasParameters());
            Assert.Equal(2, client.ResourceSubscriptionCount);
        });
        Assert.Single(cut.FindComponents<ResourceNavigationAvailabilityProvider>());
        await cut.InvokeAsync(() => cut.Instance.SwitchDashboardRunAsync(null));
        cut.WaitForAssertion(() =>
        {
            Assert.True(HasTerminals());
            Assert.True(HasParameters());
            Assert.True(shortcuts.IsShortcutAvailable(AspireKeyboardShortcut.GoToTerminals));
        });
        Assert.Equal(3, client.ResourceSubscriptionCount);

        bool HasTerminals() => isDesktop
            ? cut.FindComponent<DesktopNavMenu>().Instance.HasResourceTerminals
            : cut.FindComponent<MobileNavMenu>().Instance.HasResourceTerminals;

        bool HasParameters() => isDesktop
            ? cut.FindComponent<DesktopNavMenu>().Instance.HasParameters
            : cut.FindComponent<MobileNavMenu>().Instance.HasParameters;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task ParametersNavigation_TracksResourcesIncludingHiddenParameters(bool isDesktop, bool hidden)
    {
        var updates = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var client = new TestDashboardClient(isEnabled: true, resourceChannelProvider: () => updates);
        SetupMainLayoutServices(dashboardClient: client);
        var cut = Render<MainLayout>(builder => builder.Add(p => p.ViewportInformation,
            new ViewportInformation(IsDesktop: isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false)));
        if (!isDesktop)
        {
            await cut.InvokeAsync(() => cut.Find($"#{MainLayout.NavigationButtonId}").Click());
        }
        cut.WaitForAssertion(() => Assert.Equal(1, client.ResourceSubscriptionCount));
        AssertNavigation(false);

        var parameter = ModelTestHelpers.CreateResource("parameter", resourceType: KnownResourceTypes.Parameter, hidden: hidden);
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, parameter)]);
        cut.WaitForAssertion(() => AssertNavigation(true));
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("parameter"))]);
        cut.WaitForAssertion(() => AssertNavigation(false));
        var secondParameter = ModelTestHelpers.CreateResource("second-parameter", resourceType: KnownResourceTypes.Parameter);
        await updates.Writer.WriteAsync([
            new(ResourceViewModelChangeType.Upsert, parameter),
            new(ResourceViewModelChangeType.Upsert, secondParameter)
        ]);
        cut.WaitForAssertion(() => AssertNavigation(true));
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Delete, parameter)]);
        cut.WaitForAssertion(() => AssertNavigation(true));
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Delete, secondParameter)]);
        cut.WaitForAssertion(() => AssertNavigation(false));

        void AssertNavigation(bool expected)
        {
            var parameterCount = isDesktop
                ? cut.FindAll("#main-rail-parameters").Count
                : cut.FindAll("fluent-menu-item").Count(item => item.GetAttribute("data-tooltip") == Resources.Layout.NavMenuParametersTab);
            Assert.Equal(expected ? 1 : 0, parameterCount);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task ResourceTerminalsNavigation_TracksCapabilitiesIncludingHiddenResources(bool isDesktop, bool hidden)
    {
        var updates = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var client = new TestDashboardClient(isEnabled: true, resourceChannelProvider: () => updates);
        SetupMainLayoutServices(dashboardClient: client);
        var cut = Render<MainLayout>(builder => builder.Add(p => p.ViewportInformation,
            new ViewportInformation(IsDesktop: isDesktop, IsUltraLowHeight: false, IsUltraLowWidth: false)));
        var shortcuts = Services.GetRequiredService<ShortcutManager>();
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        if (!isDesktop)
        {
            await cut.InvokeAsync(() => cut.Find($"#{MainLayout.NavigationButtonId}").Click());
        }
        AssertNavigation(false);
        await AssertShortcutNavigationAsync(false);
        var resource = TerminalSetupHelpers.CreateTerminalResource("shell", hidden: hidden);
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, resource)]);
        cut.WaitForAssertion(() => AssertNavigation(true));
        await AssertShortcutNavigationAsync(true);
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, ModelTestHelpers.CreateResource("shell"))]);
        cut.WaitForAssertion(() => AssertNavigation(false));
        await AssertShortcutNavigationAsync(false);
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Upsert, resource)]);
        cut.WaitForAssertion(() => AssertNavigation(true));
        await AssertShortcutNavigationAsync(true);
        await updates.Writer.WriteAsync([new(ResourceViewModelChangeType.Delete, resource)]);
        cut.WaitForAssertion(() => AssertNavigation(false));
        await AssertShortcutNavigationAsync(false);

        void AssertNavigation(bool expected)
        {
            Assert.Equal(expected, cut.Instance.SubscribedShortcuts.Contains(AspireKeyboardShortcut.GoToTerminals));
            Assert.Equal(expected, shortcuts.IsShortcutAvailable(AspireKeyboardShortcut.GoToTerminals));
            if (isDesktop)
            {
                Assert.Equal(expected, cut.FindComponent<DesktopNavMenu>().Instance.HasResourceTerminals);
            }
            else
            {
                Assert.Equal(expected, cut.FindComponent<MobileNavMenu>().Instance.HasResourceTerminals);
            }
        }

        async Task AssertShortcutNavigationAsync(bool expected)
        {
            await cut.InvokeAsync(() => navigationManager.NavigateTo(DashboardUrls.MetricsUrl()));
            await cut.InvokeAsync(() => shortcuts.OnGlobalKeyDown(AspireKeyboardShortcut.GoToTerminals));
            Assert.Equal(
                navigationManager.ToAbsoluteUri(expected ? DashboardUrls.TerminalsUrl() : DashboardUrls.MetricsUrl()).AbsoluteUri,
                navigationManager.Uri);
        }
    }
}
