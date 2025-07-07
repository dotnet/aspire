// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.DashboardService.Proto.V1;
using Bunit;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Interactions;

[UseCulture("en-US")]
public partial class InteractionsProviderTests : DashboardTestContext
{
    private readonly ITestOutputHelper _testOutputHelper;

    public InteractionsProviderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Initialize_DashboardClientNotEnabled_ProviderDisabledAsync()
    {
        // Arrange
        var dashboardClient = new TestDashboardClient(isEnabled: false);

        SetupInteractionProviderServices(dashboardClient);

        // Act
        var cut = RenderComponent<Components.Interactions.InteractionsProvider>();

        var instance = cut.Instance;

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.False(instance._enabled);
        });

        await instance.DisposeAsync().DefaultTimeout();
    }

    [Fact]
    public async Task Initialize_DashboardClientEnabled_ProviderEnabledAsync()
    {
        // Arrange
        var interactionsChannel = Channel.CreateUnbounded<WatchInteractionsResponseUpdate>();

        var dashboardClient = new TestDashboardClient(isEnabled: true, interactionChannelProvider: () => interactionsChannel);

        SetupInteractionProviderServices(dashboardClient);

        // Act
        var cut = RenderComponent<Components.Interactions.InteractionsProvider>();

        var instance = cut.Instance;

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.True(instance._enabled);
        });

        await instance.DisposeAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ReceiveData_MessageBoxOpen_OpenDialog()
    {
        // Arrange
        var interactionsChannel = Channel.CreateUnbounded<WatchInteractionsResponseUpdate>();

        var dialogReference = new DialogReference("abc", null!);
        var dashboardClient = new TestDashboardClient(isEnabled: true, interactionChannelProvider: () => interactionsChannel);
        var dialogService = new TestDialogService(onShowDialog: (data, parameters) => Task.FromResult<IDialogReference>(dialogReference));

        SetupInteractionProviderServices(dashboardClient: dashboardClient, dialogService: dialogService);

        // Act 1
        var cut = RenderComponent<Components.Interactions.InteractionsProvider>();

        var instance = cut.Instance;

        await interactionsChannel.Writer.WriteAsync(new WatchInteractionsResponseUpdate
        {
            InteractionId = 1,
            MessageBox = new InteractionMessageBox()
        });

        // Assert 1
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() =>
        {
            var reference = instance._interactionDialogReference;
            if (reference == null)
            {
                return false;
            }

            return dialogReference == reference.Dialog && reference.InteractionId == 1;
        }, "Wait for dialog reference created.");

        // Act 2
        dialogReference.Dismiss(DialogResult.Ok(true));

        // Assert 2
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() => instance._interactionDialogReference == null, "Wait for dialog reference dismissed.");

        await instance.DisposeAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ReceiveData_MessageBoxOpenAndCompletion_OpenAndCloseDialog()
    {
        // Arrange
        var interactionsChannel = Channel.CreateUnbounded<WatchInteractionsResponseUpdate>();

        var dialogReference = new DialogReference("abc", null!);
        var dashboardClient = new TestDashboardClient(isEnabled: true, interactionChannelProvider: () => interactionsChannel);
        var dialogService = new TestDialogService(onShowDialog: (data, parameters) => Task.FromResult<IDialogReference>(dialogReference));

        SetupInteractionProviderServices(dashboardClient: dashboardClient, dialogService: dialogService);

        // Act 1
        var cut = RenderComponent<Components.Interactions.InteractionsProvider>();

        var instance = cut.Instance;

        await interactionsChannel.Writer.WriteAsync(new WatchInteractionsResponseUpdate
        {
            InteractionId = 1,
            MessageBox = new InteractionMessageBox()
        });

        // Assert 1
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() =>
        {
            var reference = instance._interactionDialogReference;
            if (reference == null)
            {
                return false;
            }

            return dialogReference == reference.Dialog && reference.InteractionId == 1;
        }, "Wait for dialog reference created.");

        // Act 2
        await interactionsChannel.Writer.WriteAsync(new WatchInteractionsResponseUpdate
        {
            InteractionId = 1,
            Complete = new InteractionComplete()
        });

        // Assert 2
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() => instance._interactionDialogReference == null, "Wait for dialog reference dismissed.");

        await instance.DisposeAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ReceiveData_InputDialogOpenAndCancel_OpenDialogAndSendCompletion()
    {
        // Arrange
        var interactionsChannel = Channel.CreateUnbounded<WatchInteractionsResponseUpdate>();
        var sendInteractionUpdatesChannel = Channel.CreateUnbounded<WatchInteractionsRequestUpdate>();

        DialogParameters? dialogParameters = null;
        var dialogReference = new DialogReference("abc", null!);
        var dashboardClient = new TestDashboardClient(isEnabled: true,
            interactionChannelProvider: () => interactionsChannel,
            sendInteractionUpdateChannel: sendInteractionUpdatesChannel);
        var dialogService = new TestDialogService(onShowDialog: (data, parameters) =>
        {
            dialogParameters = parameters;
            return Task.FromResult<IDialogReference>(dialogReference);
        });

        SetupInteractionProviderServices(dashboardClient: dashboardClient, dialogService: dialogService);

        // Act 1
        var cut = RenderComponent<Components.Interactions.InteractionsProvider>();

        var instance = cut.Instance;

        await interactionsChannel.Writer.WriteAsync(new WatchInteractionsResponseUpdate
        {
            InteractionId = 1,
            InputsDialog = new InteractionInputsDialog()
        });

        // Assert 1
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() =>
        {
            var reference = instance._interactionDialogReference;
            if (reference == null)
            {
                return false;
            }

            return dialogReference == reference.Dialog && reference.InteractionId == 1;
        }, "Wait for dialog reference created.");

        // Act 2
        Assert.NotNull(dialogParameters);

        await cut.InvokeAsync(() => dialogParameters.OnDialogResult.InvokeAsync(DialogResult.Cancel())).DefaultTimeout();

        var update = await sendInteractionUpdatesChannel.Reader.ReadAsync();

        Assert.Equal(1, update.InteractionId);
        Assert.Equal(WatchInteractionsRequestUpdate.KindOneofCase.Complete, update.KindCase);

        await instance.DisposeAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ReceiveData_InputDialogOpenAndSubmit_OpenDialogAndSendCompletion()
    {
        // Arrange
        var interactionsChannel = Channel.CreateUnbounded<WatchInteractionsResponseUpdate>();
        var sendInteractionUpdatesChannel = Channel.CreateUnbounded<WatchInteractionsRequestUpdate>();

        InteractionsInputsDialogViewModel? vm = null;
        DialogParameters? dialogParameters = null;
        var dialogReference = new DialogReference("abc", null!);
        var dashboardClient = new TestDashboardClient(isEnabled: true,
            interactionChannelProvider: () => interactionsChannel,
            sendInteractionUpdateChannel: sendInteractionUpdatesChannel);
        var dialogService = new TestDialogService(onShowDialog: (data, parameters) =>
        {
            vm = (InteractionsInputsDialogViewModel)data;
            dialogParameters = parameters;
            return Task.FromResult<IDialogReference>(dialogReference);
        });

        SetupInteractionProviderServices(dashboardClient: dashboardClient, dialogService: dialogService);

        // Act 1
        var cut = RenderComponent<Components.Interactions.InteractionsProvider>();

        var instance = cut.Instance;

        var response = new WatchInteractionsResponseUpdate
        {
            InteractionId = 1,
            InputsDialog = new InteractionInputsDialog()
        };
        await interactionsChannel.Writer.WriteAsync(response);

        // Assert 1
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() =>
        {
            var reference = instance._interactionDialogReference;
            if (reference == null)
            {
                return false;
            }

            return dialogReference == reference.Dialog && reference.InteractionId == 1;
        }, "Wait for dialog reference created.");

        // Act 2
        Assert.NotNull(dialogParameters);
        Assert.NotNull(vm);

        await vm.OnSubmitCallback(response).DefaultTimeout();

        var update = await sendInteractionUpdatesChannel.Reader.ReadAsync();

        Assert.Equal(1, update.InteractionId);
        Assert.Equal(WatchInteractionsRequestUpdate.KindOneofCase.InputsDialog, update.KindCase);

        await instance.DisposeAsync().DefaultTimeout();
    }

    [Theory]
    [InlineData(true, "**Hello** _World_! <b>Bold</b>", "<strong>Hello</strong> <em>World</em>! &lt;b&gt;Bold&lt;/b&gt;")]
    [InlineData(false, "**Hello** _World_! <b>Bold</b>", "**Hello** _World_! &lt;b&gt;Bold&lt;/b&gt;")]
    [InlineData(true, "Para1\r\n\r\nPara2", "<p>Para1</p>\r\n<p>Para2</p>")]
    public async Task ReceiveData_InputDialogWithMarkdownMessage_ExpectedResolvedMessage(bool markdownSupported, string message, string expectedMessage)
    {
        // Arrange
        var interactionsChannel = Channel.CreateUnbounded<WatchInteractionsResponseUpdate>();
        var sendInteractionUpdatesChannel = Channel.CreateUnbounded<WatchInteractionsRequestUpdate>();

        InteractionsInputsDialogViewModel? vm = null;
        DialogParameters? dialogParameters = null;
        var dialogReference = new DialogReference("abc", null!);
        var dashboardClient = new TestDashboardClient(isEnabled: true,
            interactionChannelProvider: () => interactionsChannel,
            sendInteractionUpdateChannel: sendInteractionUpdatesChannel);
        var dialogService = new TestDialogService(onShowDialog: (data, parameters) =>
        {
            vm = (InteractionsInputsDialogViewModel)data;
            dialogParameters = parameters;
            return Task.FromResult<IDialogReference>(dialogReference);
        });

        SetupInteractionProviderServices(dashboardClient: dashboardClient, dialogService: dialogService);

        // Act
        var cut = RenderComponent<Components.Interactions.InteractionsProvider>();

        var instance = cut.Instance;

        var response = new WatchInteractionsResponseUpdate
        {
            InteractionId = 1,
            Message = message,
            EnableMessageMarkdown = markdownSupported,
            InputsDialog = new InteractionInputsDialog()
        };
        await interactionsChannel.Writer.WriteAsync(response);

        // Assert
        await AsyncTestHelpers.AssertIsTrueRetryAsync(() =>
        {
            var reference = instance._interactionDialogReference;
            if (reference == null)
            {
                return false;
            }

            return dialogReference == reference.Dialog && reference.InteractionId == 1;
        }, "Wait for dialog reference created.");

        Assert.NotNull(vm);

        Assert.Equal(expectedMessage, vm.Message.Trim(), ignoreLineEndingDifferences: true);

        await instance.DisposeAsync().DefaultTimeout();
    }

    private void SetupInteractionProviderServices(TestDashboardClient? dashboardClient = null, TestDialogService? dialogService = null)
    {
        var loggerFactory = IntegrationTestHelpers.CreateLoggerFactory(_testOutputHelper);

        Services.AddLocalization();
        Services.AddSingleton<ILoggerFactory>(loggerFactory);

        Services.AddSingleton<IDialogService>(dialogService ?? new TestDialogService());
        Services.AddSingleton<IMessageService, MessageService>();
        Services.AddSingleton<IDashboardClient>(dashboardClient ?? new TestDashboardClient());
    }
}
