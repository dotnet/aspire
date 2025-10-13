// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Assistant.Ghcp;
using Aspire.Dashboard.Model.Assistant.Prompts;
using Aspire.Dashboard.Resources;

namespace Aspire.Dashboard.Tests;

public class TestAIContextProvider : IAIContextProvider
{
    public AssistantChatViewModel? AssistantChatViewModel { get; set; }
    public bool ShowAssistantSidebarDialog { get; set; }
    public bool Enabled { get; }
    public AssistantChatState? ChatState { get; set; }
    public IceBreakersBuilder IceBreakersBuilder { get; } = new IceBreakersBuilder(new TestStringLocalizer<AIPrompts>());

    public AIContext AddNew(string description, Action<AIContext> configure)
    {
        return new AIContext(this, raiseChange: () => { }) { Description = description };
    }

    public AIContext? GetContext()
    {
        return null;
    }

    public Task<GhcpInfoResponse> GetInfoAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task HideAssistantSidebarAsync()
    {
        throw new NotImplementedException();
    }

    public Task LaunchAssistantModelDialogAsync(AssistantChatViewModel viewModel, bool openedForMobileView = false)
    {
        throw new NotImplementedException();
    }

    public Task LaunchAssistantSidebarAsync(AssistantChatViewModel viewModel)
    {
        throw new NotImplementedException();
    }

    public Task LaunchAssistantSidebarAsync(Func<InitializePromptContext, Task> sendInitialPrompt)
    {
        throw new NotImplementedException();
    }

    public IDisposable OnContextChanged(Func<Task> callback)
    {
        throw new NotImplementedException();
    }

    public IDisposable OnDisplayChanged(Func<Task> callback)
    {
        return new DisplayChangedSubscription();
    }

    public void Remove(AIContext context)
    {
    }

    public Task SetAssistantSidebarAsync(AssistantChatViewModel viewModel)
    {
        throw new NotImplementedException();
    }

    private sealed class DisplayChangedSubscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
