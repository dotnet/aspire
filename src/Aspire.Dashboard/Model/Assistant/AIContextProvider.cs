// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model.Assistant.Ghcp;
using Aspire.Dashboard.Model.Assistant.Prompts;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model.Assistant;

public class AIContextProvider : IAIContextProvider
{
    private readonly object _lock = new object();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIContextProvider> _logger;
    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptions;
    private readonly ChatClientFactory _chatClientFactory;
    private readonly List<AIContext> _contextsStack = new List<AIContext>();
    private readonly List<ModelSubscription> _contextChangedSubscriptions = [];
    private readonly List<ModelSubscription> _displayChangedSubscriptions = [];
    private GhcpInfoResponse? _response;

    public AIContextProvider(
        IServiceProvider serviceProvider,
        ILogger<AIContextProvider> logger,
        IOptionsMonitor<DashboardOptions> dashboardOptions,
        ChatClientFactory chatClientFactory,
        IceBreakersBuilder iceBreakersBuilder)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _dashboardOptions = dashboardOptions;
        _chatClientFactory = chatClientFactory;
        IceBreakersBuilder = iceBreakersBuilder;
        Enabled = IsEnabled();
    }

    public bool Enabled { get; }
    public AssistantChatViewModel? AssistantChatViewModel { get; private set; }
    public bool ShowAssistantSidebarDialog { get; private set; }
    public AssistantChatState? ChatState { get; set; }
    public IceBreakersBuilder IceBreakersBuilder { get; }

    public AIContext? GetContext()
    {
        lock (_lock)
        {
            if (_contextsStack.Count == 0)
            {
                return null;
            }

            return _contextsStack[_contextsStack.Count - 1];
        }
    }

    public AIContext AddNew(string description, Action<AIContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        AIContext context = null!;
        context = new AIContext(this, () => RaiseContextChange(context))
        {
            Description = description
        };
        configure(context);

        lock (_lock)
        {
            _contextsStack.Add(context);
        }

        ExecuteSubscriptions(_contextChangedSubscriptions);

        return context;
    }

    private void RaiseContextChange(AIContext context)
    {
        // If the context raises a change, and it's the current context, then notify the subscribers.
        var contextChanged = false;
        lock (_lock)
        {
            var index = _contextsStack.IndexOf(context);
            if (index == _contextsStack.Count - 1)
            {
                contextChanged = true;
            }
        }
        if (contextChanged)
        {
            ExecuteSubscriptions(_contextChangedSubscriptions);
        }
    }

    private void ExecuteSubscriptions(List<ModelSubscription> subscriptions)
    {
        // Execute subscriptions in Task.Run to avoid making AddNew async.
        // If this is a problem then AddNew could probably be made async and the caller could await it.
        _ = Task.Run(() => ExecuteSubscriptionsAsync(subscriptions));
    }

    private async Task ExecuteSubscriptionsAsync(List<ModelSubscription> subscriptions)
    {
        try
        {
            List<ModelSubscription> subscriptionsCopy;
            lock (_lock)
            {
                subscriptionsCopy = subscriptions.ToList();
            }

            foreach (var subscription in subscriptionsCopy)
            {
                await subscription.ExecuteAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while executing subscriptions.");
        }
    }

    public void Remove(AIContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var currentContextChanged = false;
        lock (_lock)
        {
            var index = _contextsStack.IndexOf(context);
            if (index == -1)
            {
                return;
            }
            else if (index == _contextsStack.Count - 1)
            {
                // Context removed was the current context.
                currentContextChanged = true;
            }

            _contextsStack.RemoveAt(index);
        }

        if (currentContextChanged)
        {
            ExecuteSubscriptions(_contextChangedSubscriptions);
        }
    }

    // Internal for testing
    internal int ProviderCount
    {
        get
        {
            lock (_lock)
            {
                return _contextsStack.Count;
            }
        }
    }

    // Internal for testing
    internal int SubscriptionCount
    {
        get
        {
            lock (_lock)
            {
                return _contextChangedSubscriptions.Count;
            }
        }
    }

    public async Task LaunchAssistantSidebarAsync(Func<InitializePromptContext, Task> sendInitialPrompt)
    {
        this.EnsureEnabled();

        var viewModel = _serviceProvider.GetRequiredService<AssistantChatViewModel>();
        var initializeTask = viewModel.InitializeWithInitialPromptAsync(async () =>
        {
            var chatBuilder = new ChatViewModelBuilder(viewModel.MarkdownProcessor);
            await sendInitialPrompt(new InitializePromptContext(chatBuilder, viewModel.DataContext, _serviceProvider)).ConfigureAwait(false);

            await viewModel.AddFollowUpPromptAsync(chatBuilder.Build()).ConfigureAwait(false);
        });

        AssistantChatViewModel = viewModel;
        ShowAssistantSidebarDialog = true;

        await ExecuteSubscriptionsAsync(_displayChangedSubscriptions).ConfigureAwait(false);
        await initializeTask.ConfigureAwait(false);
    }

    public IDisposable OnContextChanged(Func<Task> callback)
    {
        lock (_lock)
        {
            var subscription = new ModelSubscription(callback, RemoveContextChangedSubscription);
            _contextChangedSubscriptions.Add(subscription);
            return subscription;
        }
    }

    private void RemoveContextChangedSubscription(ModelSubscription subscription)
    {
        lock (_lock)
        {
            _contextChangedSubscriptions.Remove(subscription);
        }
    }

    public IDisposable OnDisplayChanged(Func<Task> callback)
    {
        lock (_lock)
        {
            var subscription = new ModelSubscription(callback, RemoveDisplayChangedSubscription);
            _displayChangedSubscriptions.Add(subscription);
            return subscription;
        }
    }

    private void RemoveDisplayChangedSubscription(ModelSubscription subscription)
    {
        lock (_lock)
        {
            _displayChangedSubscriptions.Remove(subscription);
        }
    }

    public async Task SetAssistantSidebarAsync(AssistantChatViewModel viewModel)
    {
        AssistantChatViewModel = viewModel;
        await ExecuteSubscriptionsAsync(_displayChangedSubscriptions).ConfigureAwait(false);
    }

    public async Task HideAssistantSidebarAsync()
    {
        AssistantChatViewModel = null;
        ShowAssistantSidebarDialog = false;
        await ExecuteSubscriptionsAsync(_displayChangedSubscriptions).ConfigureAwait(false);
    }

    public async Task LaunchAssistantModelDialogAsync(AssistantChatViewModel viewModel, bool openedForMobileView = false)
    {
        this.EnsureEnabled();

        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        await AssistantModalDialog.OpenDialogAsync(dialogService, "Assistant", new AssistantDialogViewModel
        {
            Chat = viewModel,
            OpenedForMobileView = openedForMobileView
        }).ConfigureAwait(false);
        await ExecuteSubscriptionsAsync(_displayChangedSubscriptions).ConfigureAwait(false);
    }

    public async Task LaunchAssistantSidebarAsync(AssistantChatViewModel viewModel)
    {
        this.EnsureEnabled();

        AssistantChatViewModel = viewModel;
        ShowAssistantSidebarDialog = true;
        await ExecuteSubscriptionsAsync(_displayChangedSubscriptions).ConfigureAwait(false);
    }

    public async Task<GhcpInfoResponse> GetInfoAsync(CancellationToken cancellationToken)
    {
        _response ??= await _chatClientFactory.GetInfoAsync(cancellationToken).ConfigureAwait(false);

        return _response;
    }

    private bool IsEnabled()
    {
        // Explicitly disable AI in configuration.
        if (_dashboardOptions.CurrentValue.AI.Disabled.GetValueOrDefault())
        {
            _logger.LogInformation("AI is disabled in configuration.");
            return false;
        }

        // Check if the client factory has the right configuration to be enabled.
        return _chatClientFactory.IsEnabled();
    }
}
