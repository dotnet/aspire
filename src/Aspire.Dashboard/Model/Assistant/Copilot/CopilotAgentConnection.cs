// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using GitHub.Copilot.SDK;

namespace Aspire.Dashboard.Model.Assistant.Copilot;

/// <summary>
/// Implementation of <see cref="IAgentConnection"/> using the Copilot CLI SDK.
/// </summary>
internal sealed class CopilotAgentConnection : IAgentConnection
{
    private readonly CopilotSession _session;
    private readonly ILogger<CopilotAgentConnection> _logger;
    private readonly List<Action<AgentEvent>> _handlers = new();
    private readonly object _lock = new();
    private bool _disposed;

    public CopilotAgentConnection(CopilotSession session, ILogger<CopilotAgentConnection> logger)
    {
        _session = session;
        _logger = logger;

        // Subscribe to SDK events and map them to our AgentEvent types
        _session.On(HandleSdkEvent);
    }

    public string SessionId => _session.SessionId;

#pragma warning disable CA2016 // SDK doesn't support cancellation tokens
    public async Task SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogDebug("Sending message to agent session {SessionId}", SessionId);

        // SDK doesn't support cancellation tokens directly, so we handle it manually
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _session.SendAsync(new MessageOptions { Prompt = prompt }).ConfigureAwait(false);
    }
#pragma warning restore CA2016

    public async Task AbortAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogDebug("Aborting agent session {SessionId}", SessionId);

        await _session.AbortAsync().ConfigureAwait(false);
    }

    public IDisposable OnEvent(Action<AgentEvent> handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _handlers.Add(handler);
        }

        return new EventSubscription(this, handler);
    }

    private void HandleSdkEvent(SessionEvent evt)
    {
        var agentEvent = MapToAgentEvent(evt);
        if (agentEvent is null)
        {
            return;
        }

        List<Action<AgentEvent>> handlers;
        lock (_lock)
        {
            handlers = _handlers.ToList();
        }

        foreach (var handler in handlers)
        {
            try
            {
                handler(agentEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent event handler");
            }
        }
    }

    private static AgentEvent? MapToAgentEvent(SessionEvent evt)
    {
        return evt switch
        {
            AssistantMessageDeltaEvent delta => new AgentTextDeltaEvent
            {
                DeltaContent = delta.Data.DeltaContent ?? string.Empty
            },
            AssistantMessageEvent msg => new AgentTextCompleteEvent
            {
                Content = msg.Data.Content ?? string.Empty
            },
            ToolExecutionStartEvent toolStart => new AgentToolStartEvent
            {
                ToolName = toolStart.Data.ToolName,
                Description = null // SDK doesn't provide description
            },
            ToolExecutionCompleteEvent toolComplete => new AgentToolCompleteEvent
            {
                ToolName = toolComplete.Data.ToolCallId, // Use ToolCallId as identifier
                Success = toolComplete.Data.Success
            },
            SessionIdleEvent => new AgentIdleEvent(),
            SessionErrorEvent error => new AgentErrorEvent
            {
                Message = error.Data.Message ?? "Unknown error",
                IsRecoverable = false
            },
            AssistantReasoningDeltaEvent reasoningDelta => new AgentReasoningDeltaEvent
            {
                DeltaContent = reasoningDelta.Data.DeltaContent ?? string.Empty
            },
            AssistantReasoningEvent reasoning => new AgentReasoningCompleteEvent
            {
                Content = reasoning.Data.Content ?? string.Empty
            },
            _ => null // Ignore other event types
        };
    }

    private void RemoveHandler(Action<AgentEvent> handler)
    {
        lock (_lock)
        {
            _handlers.Remove(handler);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        lock (_lock)
        {
            _handlers.Clear();
        }

        await _session.DisposeAsync().ConfigureAwait(false);

        _logger.LogDebug("Disposed agent connection {SessionId}", SessionId);
    }

    private sealed class EventSubscription : IDisposable
    {
        private readonly CopilotAgentConnection _connection;
        private readonly Action<AgentEvent> _handler;

        public EventSubscription(CopilotAgentConnection connection, Action<AgentEvent> handler)
        {
            _connection = connection;
            _handler = handler;
        }

        public void Dispose()
        {
            _connection.RemoveHandler(_handler);
        }
    }
}
