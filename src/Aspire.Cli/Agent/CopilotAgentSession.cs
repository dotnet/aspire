// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Agent;

/// <summary>
/// Agent session implementation using the GitHub Copilot SDK.
/// </summary>
internal sealed class CopilotAgentSession : IAgentSession
{
    private readonly IAgentToolRegistry _toolRegistry;
    private readonly ILogger<CopilotAgentSession> _logger;
    private CopilotClient? _client;
    private CopilotSession? _session;
    private AgentContext _context = new();

    public CopilotAgentSession(
        IAgentToolRegistry toolRegistry,
        ILogger<CopilotAgentSession> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public AgentContext Context => _context;

    public bool IsConnected => _session is not null;

    public async Task InitializeAsync(FileInfo? appHostProject, CancellationToken cancellationToken = default)
    {
        _context = new AgentContext
        {
            AppHostProject = appHostProject,
            WorkingDirectory = new DirectoryInfo(Environment.CurrentDirectory)
        };

        try
        {
            _client = new CopilotClient(new CopilotClientOptions
            {
                AutoStart = true,
                Logger = _logger
            });

            await _client.StartAsync(cancellationToken);

            var tools = _toolRegistry.GetTools(_context);

            _session = await _client.CreateSessionAsync(new SessionConfig
            {
                Model = "claude-opus-4",
                Streaming = true,
                Tools = tools,
                SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Append,
                    Content = GetSystemPrompt()
                }
            }, cancellationToken);

            _logger.LogDebug("Agent session initialized with {ToolCount} tools", tools.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize agent session");
            throw new AgentSessionException("Failed to connect to Copilot CLI. Ensure 'copilot' is installed and you're authenticated with 'gh auth login'.", ex);
        }
    }

    public async Task SendMessageAsync(string prompt, Action<AgentEvent> onEvent, CancellationToken cancellationToken = default)
    {
        if (_session is null)
        {
            throw new AgentSessionException("Session not initialized. Call InitializeAsync first.");
        }

        var tcs = new TaskCompletionSource();

        using var subscription = _session.On(evt =>
        {
            var agentEvent = MapSessionEvent(evt);
            if (agentEvent is not null)
            {
                onEvent(agentEvent);
            }

            if (agentEvent is SessionIdleEvent or SessionErrorEvent)
            {
                tcs.TrySetResult();
            }
        });

        await _session.SendAsync(new MessageOptions { Prompt = prompt }, cancellationToken);

        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
        await tcs.Task;
    }

    public async Task AbortAsync()
    {
        if (_session is not null)
        {
            await _session.AbortAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.DisposeAsync();
            _session = null;
        }

        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
    }

    private static AgentEvent? MapSessionEvent(GitHub.Copilot.SDK.SessionEvent evt)
    {
        return evt switch
        {
            GitHub.Copilot.SDK.AssistantTurnStartEvent => new AssistantTurnStartEvent(),
            GitHub.Copilot.SDK.AssistantMessageDeltaEvent delta => new AssistantMessageDeltaEvent(delta.Data?.DeltaContent ?? ""),
            GitHub.Copilot.SDK.AssistantMessageEvent msg => new AssistantMessageEvent(msg.Data?.Content ?? ""),
            GitHub.Copilot.SDK.ToolExecutionStartEvent toolStart => new ToolExecutionStartEvent(toolStart.Data?.ToolName ?? "", toolStart.Data?.Arguments?.ToString()),
            GitHub.Copilot.SDK.ToolExecutionCompleteEvent toolEnd => new ToolExecutionCompleteEvent(toolEnd.Data?.ToolCallId ?? "", toolEnd.Data?.Result?.Content, toolEnd.Data?.Success == true),
            GitHub.Copilot.SDK.SessionIdleEvent => new SessionIdleEvent(),
            GitHub.Copilot.SDK.SessionErrorEvent err => new SessionErrorEvent(err.Data?.Message ?? "Unknown error"),
            _ => null
        };
    }

    private string GetSystemPrompt()
    {
        return $"""
            You are the Aspire Agent, an AI assistant specialized in building Aspire 
            distributed applications. You help developers:

            - Create new Aspire projects with appropriate templates
            - Add and configure integrations (databases, caches, message queues, Azure services)
            - Connect integrations together (e.g., a web app to a database, a worker to a message queue)
            - Run and debug applications locally
            - Deploy to Azure and other cloud environments
            - Troubleshoot issues using logs, traces, and diagnostics

            You have deep knowledge of Aspire integrations:
            - **Hosting integrations** (AppHost): How to add resources like Redis, PostgreSQL, RabbitMQ, Azure services
            - **Client integrations** (Service projects): How services consume resources via dependency injection
            - **Connection patterns**: How WithReference() wires up connection strings and service discovery
            - **Integration pairs**: Which hosting package pairs with which client package

            Current project context:
            {_context.GetContextSummary()}

            Guidelines:
            - Be concise and actionable
            - Prefer using tools over explaining how to do things manually
            - When adding integrations, explain BOTH the hosting side (AppHost) AND the client side (service project)
            - Always show how to wire up connections using WithReference()
            - When adding resources, verify the AppHost was updated correctly
            - For troubleshooting, start with `doctor` then check logs/traces
            - Explain what you're doing before executing tools
            """;
    }
}
