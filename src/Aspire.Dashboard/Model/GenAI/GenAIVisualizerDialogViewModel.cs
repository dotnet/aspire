// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    public required long? SelectedLogEntryId { get; init; }
    public required Func<List<OtlpSpan>> GetContextGenAISpans { get; init; }

    public string? PeerName { get; set; }
    public string? SourceName { get; set; }

    public FluentTreeItem? SelectedTreeItem { get; set; }
    public List<GenAIMessageViewModel> Messages { get; } = new List<GenAIMessageViewModel>();

    public GenAIMessageViewModel? SelectedMessage { get; set; }
    public bool HasSelectedMessage { get; set; }

    public OverviewViewKind OverviewActiveView { get; set; }
    public EventViewKind EventActiveView { get; set; }

    public string? ModelName { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }

    public static GenAIVisualizerDialogViewModel Create(
        SpanDetailsViewModel spanDetailsViewModel,
        long? selectedLogEntryId,
        TelemetryRepository telemetryRepository,
        Func<List<OtlpSpan>> getContextGenAISpans)
    {
        var logsContext = new GetLogsContext
        {
            ResourceKey = null,
            Count = int.MaxValue,
            StartIndex = 0,
            Filters = [
                new TelemetryFilter
                {
                    Field = KnownStructuredLogFields.SpanIdField,
                    Condition = FilterCondition.Equals,
                    Value = spanDetailsViewModel.Span.SpanId
                }
            ]
        };
        var logsResult = telemetryRepository.GetLogs(logsContext);

        var viewModel = new GenAIVisualizerDialogViewModel
        {
            Span = spanDetailsViewModel.Span,
            LogEntries = logsResult.Items,
            Title = SpanWaterfallViewModel.GetTitle(spanDetailsViewModel.Span, spanDetailsViewModel.Resources),
            SpanDetailsViewModel = spanDetailsViewModel,
            SelectedLogEntryId = selectedLogEntryId,
            GetContextGenAISpans = getContextGenAISpans
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
        var currentIndex = 0;

        // Attempt to get messages from log entries.
        foreach (var item in viewModel.LogEntries.OrderBy(i => i.TimeStamp))
        {
            if (item.Attributes.GetValue("event.name") is { } name && TryMapEventName(name, out var type))
            {
                var parts = DeserializeBody(type.Value, item.Message);
                viewModel.Messages.Add(CreateMessage(viewModel, currentIndex, type.Value, parts));
                currentIndex++;
            }
        }

        if (viewModel.Messages.Count > 0)
        {
            return;
        }

        // Attempt get get messages from span events.
        foreach (var item in viewModel.Span.Events.OrderBy(i => i.Time))
        {
            if (item.Attributes.HasKey("gen_ai.system") &&
                item.Attributes.GetValue("gen_ai.event.content") is { } content &&
                TryMapEventName(item.Name, out var type))
            {
                var parts = DeserializeBody(type.Value, content);
                viewModel.Messages.Add(CreateMessage(viewModel, currentIndex, type.Value, parts));
                currentIndex++;
            }
        }
    }

    private static GenAIMessageViewModel CreateMessage(GenAIVisualizerDialogViewModel viewModel, int currentIndex, GenAIEventType type, List<GenAIMessagePartViewModel> parts)
    {
        return new GenAIMessageViewModel
        {
            Index = currentIndex,
            InternalId = null,
            Type = type,
            Parent = viewModel.Span,
            ResourceName = type is GenAIEventType.AssistantMessage or GenAIEventType.Choice ? viewModel.PeerName! : viewModel.SourceName!,
            MessageParts = parts
        };
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
                var toolResponse = ProcessJsonPayload(toolEvent.Content);
                messagePartViewModels.Add(new()
                {
                    MessagePart = new ToolCallResponsePart { Id = toolEvent.Id, Response = toolResponse },
                    TextVisualizerViewModel = new TextVisualizerViewModel(toolResponse?.ToJsonString() ?? string.Empty, indentText: true)
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

                    var args = ProcessJsonPayload(function.Arguments);
                    var content = $"{function.Name}({args?.ToJsonString()})";

                    messagePartViewModels.Add(new()
                    {
                        MessagePart = new ToolCallRequestPart { Name = function.Name, Arguments = function.Arguments },
                        TextVisualizerViewModel = new TextVisualizerViewModel(content, indentText: false, format: "javascript")
                    });
                }
            }
        }
    }

    // Args might be a serialized object string instead of a raw object.
    // To avoid extra escaping in displaying serialized object string, attempt to convert to object.
    private static JsonNode? ProcessJsonPayload(JsonNode? args)
    {
        if (args?.GetValueKind() == JsonValueKind.String && args.GetValue<string>() is { } argsJson)
        {
            try
            {
                var node = JsonNode.Parse(argsJson);
                if (node?.GetValueKind() is JsonValueKind.Object or JsonValueKind.Array)
                {
                    args = node;
                }
            }
            catch (Exception)
            {
                // Not a JSON string. Ignore.
            }
        }

        return args;
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
