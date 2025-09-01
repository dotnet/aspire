// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model.GenAI;

public enum GenAIEventType
{
    SystemMessage,
    UserMessage,
    AssistantMessage,
    ToolMessage,
    Choice
}

public class GenAIVisualizerDialogViewModel
{
    public required OtlpSpan Span { get; init; }
    public required List<OtlpLogEntry> LogEntries { get; init; }
    public required string Title { get; init; }
    public required SpanDetailsViewModel SpanDetailsViewModel { get; init; }
    public required long? SelectedLogEntryId { get; set; }

    public string? PeerName { get; set; }
    public string? SourceName { get; set; }

    public FluentTreeItem? SelectedTreeItem { get; set; }
    public List<GenAIMessageViewModel> Messages { get; } = new List<GenAIMessageViewModel>();

    public GenAIMessageViewModel? SelectedMessage { get; set; }

    public OverviewViewKind OverviewActiveView { get; set; }
    public EventViewKind EventActiveView { get; set; }

    public string? ModelName { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }

    public static GenAIVisualizerDialogViewModel Create(List<OtlpLogEntry> logEntries, SpanDetailsViewModel spanDetailsViewModel, long? selectedLogEntryId, TelemetryRepository telemetryRepository)
    {
        var viewModel = new GenAIVisualizerDialogViewModel
        {
            Span = spanDetailsViewModel.Span,
            LogEntries = logEntries,
            Title = SpanWaterfallViewModel.GetTitle(spanDetailsViewModel.Span, spanDetailsViewModel.Resources),
            SpanDetailsViewModel = spanDetailsViewModel,
            SelectedLogEntryId = selectedLogEntryId
        };

        var resources = telemetryRepository.GetResources();
        viewModel.SourceName = OtlpResource.GetResourceName(viewModel.Span.Source, resources);

        if (telemetryRepository.GetPeerResource(viewModel.Span) is { } peerResource)
        {
            viewModel.PeerName = OtlpResource.GetResourceName(peerResource, resources);
        }
        else
        {
            viewModel.PeerName = OtlpHelpers.GetPeerAddress(viewModel.Span.Attributes)!;
        }

        viewModel.ModelName = viewModel.Span.Attributes.GetValue("gen_ai.response.model");
        viewModel.InputTokens = viewModel.Span.Attributes.GetValueAsInteger("gen_ai.usage.input_tokens");
        viewModel.OutputTokens = viewModel.Span.Attributes.GetValueAsInteger("gen_ai.usage.output_tokens");

        CreateMessages(viewModel);

        if (viewModel.SelectedLogEntryId != null)
        {
            viewModel.SelectedMessage = viewModel.Messages.SingleOrDefault(e => e.InternalId == viewModel.SelectedLogEntryId);
        }

        return viewModel;
    }

    private static void CreateMessages(GenAIVisualizerDialogViewModel viewModel)
    {
        // Attempt to get messages from log entries.
        foreach (var item in viewModel.LogEntries.OrderBy(i => i.TimeStamp))
        {
            if (item.Attributes.GetValue("event.name") is { } name && TryMapEventName(name, out var type))
            {
                var parts = DeserializeBody(type.Value, item.Message);

                viewModel.Messages.Add(new GenAIMessageViewModel
                {
                    InternalId = item.InternalId,
                    Type = type.Value,
                    Parent = viewModel.Span,
                    ResourceName = type.Value is GenAIEventType.AssistantMessage or GenAIEventType.Choice ? viewModel.PeerName! : viewModel.SourceName!,
                    MessageParts = parts
                });
            }
        }

        if (viewModel.Messages.Count > 0)
        {
            return;
        }

        /*
        // Attempt get get messages from span events.
        foreach (var item in viewModel.Span.Events.OrderBy(i => i.Time))
        {
            if (item.Attributes.HasKey("gen_ai.system") && item.Attributes.GetValue("gen_ai.event.content") is { } content)
            {
                content.
            }
            ;
            if (item.Attributes.GetValue("event.name") is { } name && TryMapEventName(name, out var type))
            {
                var parts = DeserializeBody(type.Value, item.Message);
                viewModel.Messages.Add(new GenAIMessageViewModel
                {
                    InternalId = null,
                    Type = type.Value,
                    Parent = viewModel.Span,
                    ResourceName = type.Value is GenAIEventType.AssistantMessage or GenAIEventType.Choice ? viewModel.PeerName! : viewModel.SourceName!,
                    MessageParts = parts
                });
            }
        }
        */
    }

    private static List<GenAIMessagePartViewModel> DeserializeBody(GenAIEventType type, string message)
    {
        var messagePartViewModels = new List<GenAIMessagePartViewModel>();

        switch (type)
        {
            case GenAIEventType.SystemMessage:
            case GenAIEventType.UserMessage:
                var systemOrUserEvent = JsonSerializer.Deserialize(message, OtelContext.Default.SystemOrUserEvent)!;
                messagePartViewModels.Add(new()
                {
                    MessagePart = new TextPart { Content = systemOrUserEvent.Content },
                    TextVisualizerViewModel = new TextVisualizerViewModel(systemOrUserEvent.Content ?? string.Empty, indentText: true)
                });
                break;
            case GenAIEventType.AssistantMessage:
                var assistantEvent = JsonSerializer.Deserialize(message, OtelContext.Default.AssistantEvent)!;
                ProcessAssistantEvent(messagePartViewModels, assistantEvent);
                break;
            case GenAIEventType.ToolMessage:
                var toolEvent = JsonSerializer.Deserialize(message, OtelContext.Default.ToolEvent)!;
                messagePartViewModels.Add(new()
                {
                    MessagePart = new ToolCallResponsePart { Id = toolEvent.Id, Response = toolEvent.Content },
                    TextVisualizerViewModel = new TextVisualizerViewModel(toolEvent.Content?.ToJsonString() ?? string.Empty, indentText: true)
                });
                break;
            case GenAIEventType.Choice:
                var choiceEvent = JsonSerializer.Deserialize(message, OtelContext.Default.ChoiceEvent)!;
                if (choiceEvent.Message is { } m)
                {
                    ProcessAssistantEvent(messagePartViewModels, m);
                }
                break;
        }

        return messagePartViewModels;

        static void ProcessAssistantEvent(List<GenAIMessagePartViewModel> messagePartViewModels, AssistantEvent assistantEvent)
        {
            if (assistantEvent.Content != null)
            {
                messagePartViewModels.Add(new()
                {
                    MessagePart = new TextPart { Content = assistantEvent.Content },
                    TextVisualizerViewModel = new TextVisualizerViewModel(assistantEvent.Content, indentText: true)
                });
            }
            if (assistantEvent?.ToolCalls?.Length > 0)
            {
                for (var i = 0; i < assistantEvent.ToolCalls.Length; i++)
                {
                    var function = assistantEvent.ToolCalls[i].Function;
                    if (function == null)
                    {
                        continue;
                    }

                    var content = $"{function.Name}({function.Arguments?.ToJsonString()})";

                    messagePartViewModels.Add(new()
                    {
                        MessagePart = new ToolCallRequestPart { Name = function.Name, Arguments = function.Arguments },
                        TextVisualizerViewModel = new TextVisualizerViewModel(content, indentText: false, format: "javascript")
                    });
                }
            }
        }
    }

    private static bool TryMapEventName(string name, [NotNullWhen(true)] out GenAIEventType? type)
    {
        type = name switch
        {
            "gen_ai.system.message" => GenAIEventType.SystemMessage,
            "gen_ai.user.message" => GenAIEventType.UserMessage,
            "gen_ai.assistant.message" => GenAIEventType.AssistantMessage,
            "gen_ai.tool.message" => GenAIEventType.ToolMessage,
            "gen_ai.choice" => GenAIEventType.Choice,
            _ => null
        };

        return type != null;
    }
}

public enum OverviewViewKind
{
    InputOutput,
    Details
}

public enum EventViewKind
{
    Preview,
    Raw,
    Toolcalls
}

public record BadgeDetail(string Text, string Class, Icon Icon);
