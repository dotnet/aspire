// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model.GenAI;

[DebuggerDisplay("Span = {Span.SpanId}, Title = {Title}, Messages = {Messages.Count}")]
public sealed class GenAIVisualizerDialogViewModel
{
    public required OtlpSpan Span { get; init; }
    public required string Title { get; init; }
    public required SpanDetailsViewModel SpanDetailsViewModel { get; init; }
    public required long? SelectedLogEntryId { get; init; }
    public required Func<List<OtlpSpan>> GetContextGenAISpans { get; init; }

    public string? PeerName { get; set; }
    public string? SourceName { get; set; }

    public FluentTreeItem? SelectedTreeItem { get; set; }
    public List<GenAIItemViewModel> Items { get; } = new List<GenAIItemViewModel>();
    public List<GenAIItemViewModel> InputMessages { get; private set; } = default!;
    public List<GenAIItemViewModel> OutputMessages { get; private set; } = default!;
    public GenAIItemViewModel? ErrorItem { get; private set; }

    public GenAIItemViewModel? SelectedItem { get; set; }

    public OverviewViewKind OverviewActiveView { get; set; }
    public ItemViewKind MessageActiveView { get; set; }

    public bool NoMessageContent { get; set; }
    public string? ModelName { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }

    public static GenAIVisualizerDialogViewModel Create(
        SpanDetailsViewModel spanDetailsViewModel,
        long? selectedLogEntryId,
        TelemetryRepository telemetryRepository,
        Func<List<OtlpSpan>> getContextGenAISpans)
    {
        var viewModel = new GenAIVisualizerDialogViewModel
        {
            Span = spanDetailsViewModel.Span,
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

        viewModel.ModelName = viewModel.Span.Attributes.GetValue(GenAIHelpers.GenAIResponseModel);
        viewModel.InputTokens = viewModel.Span.Attributes.GetValueAsInteger(GenAIHelpers.GenAIUsageInputTokens);
        viewModel.OutputTokens = viewModel.Span.Attributes.GetValueAsInteger(GenAIHelpers.GenAIUsageOutputTokens);

        CreateMessages(viewModel, telemetryRepository);

        if (viewModel.Span.Status == OtlpSpanStatusCode.Error)
        {
            var errorMessage = FormatHelpers.CombineWithSeparator(
                Environment.NewLine + Environment.NewLine,
                viewModel.Span.Attributes.GetValue("error.type"),
                viewModel.Span.StatusMessage);

            viewModel.ErrorItem = new GenAIItemViewModel
            {
                Index = viewModel.Items.Count,
                InternalId = null,
                ItemParts = [new GenAIItemPartViewModel
                {
                    MessagePart = null,
                    ErrorMessage = errorMessage,
                    TextVisualizerViewModel = new TextVisualizerViewModel(errorMessage, indentText: false)
                }],
                Parent = viewModel.Span,
                ResourceName = viewModel.PeerName,
                Type = GenAIItemType.Error
            };
            viewModel.Items.Add(viewModel.ErrorItem);
        }

        if (viewModel.SelectedLogEntryId != null)
        {
            viewModel.SelectedItem = viewModel.Items.SingleOrDefault(e => e.InternalId == viewModel.SelectedLogEntryId);
        }

        viewModel.InputMessages = viewModel.Items.Where(e => e.Type is GenAIItemType.SystemMessage or GenAIItemType.UserMessage or GenAIItemType.AssistantMessage or GenAIItemType.ToolMessage).ToList();
        viewModel.OutputMessages = viewModel.Items.Where(e => e.Type == GenAIItemType.OutputMessage).ToList();

        viewModel.NoMessageContent = AllMessagesHaveNoContent(viewModel.InputMessages) && AllMessagesHaveNoContent(viewModel.OutputMessages);

        return viewModel;
    }

    private static bool AllMessagesHaveNoContent(List<GenAIItemViewModel> messageViewModels)
    {
        if (messageViewModels.Count == 0)
        {
            return false;
        }

        foreach (var messageViewModel in messageViewModels)
        {
            foreach (var partViewModel in messageViewModel.ItemParts)
            {
                if (partViewModel.MessagePart is TextPart textPart)
                {
                    if (!string.IsNullOrEmpty(textPart.Content))
                    {
                        return false;
                    }
                }
                else if (partViewModel.MessagePart is ToolCallRequestPart toolCallRequestPart)
                {
                    if (toolCallRequestPart.Arguments != null)
                    {
                        return false;
                    }
                }
                else if (partViewModel.MessagePart is ToolCallResponsePart toolCallResponsePart)
                {
                    if (toolCallResponsePart.Response != null)
                    {
                        return false;
                    }
                }
                else if (partViewModel.MessagePart is GenericPart)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // The standard for recording messages has changed a number of times. Try to get messages in the following order:
    // - Span attributes.
    // - Log entry bodies.
    // - Span event attributes.
    private static void CreateMessages(GenAIVisualizerDialogViewModel viewModel, TelemetryRepository telemetryRepository)
    {
        var currentIndex = 0;

        var systemInstructions = viewModel.Span.Attributes.GetValue(GenAIHelpers.GenAISystemInstructions);
        var inputMessages = viewModel.Span.Attributes.GetValue(GenAIHelpers.GenAIInputMessages);
        var outputMessages = viewModel.Span.Attributes.GetValue(GenAIHelpers.GenAIOutputInstructions);

        if (systemInstructions != null || inputMessages != null || outputMessages != null)
        {
            if (systemInstructions != null)
            {
                var instructionParts = JsonSerializer.Deserialize(systemInstructions, GenAIMessagesContext.Default.ListMessagePart)!;
                viewModel.Items.Add(CreateMessage(viewModel, currentIndex, GenAIItemType.SystemMessage, instructionParts.Select(p => new GenAIItemPartViewModel
                {
                    MessagePart = p,
                    ErrorMessage = null,
                    TextVisualizerViewModel = CreateMessagePartVisualizer(p)
                }).ToList(), internalId: null));
                currentIndex++;
            }
            if (inputMessages != null)
            {
                ParseMessages(viewModel, inputMessages, isOutput: false, ref currentIndex);
            }
            if (outputMessages != null)
            {
                ParseMessages(viewModel, outputMessages, isOutput: true, ref currentIndex);
            }

            return;
        }

        // Attempt to get messages from log entries.
        var logEntries = GetSpanLogEntries(telemetryRepository, viewModel.Span);
        foreach (var item in logEntries.OrderBy(i => i.TimeStamp))
        {
            if (item.Attributes.GetValue("event.name") is { } name && TryMapEventName(name, out var type))
            {
                var parts = DeserializeBody(type.Value, item.Message);
                viewModel.Items.Add(CreateMessage(viewModel, currentIndex, type.Value, parts, internalId: item.InternalId));
                currentIndex++;
            }
        }

        if (viewModel.Items.Count > 0)
        {
            return;
        }

        // Attempt get get messages from span events.
        foreach (var item in viewModel.Span.Events.OrderBy(i => i.Time))
        {
            if (GenAIHelpers.IsGenAISpan(item.Attributes) &&
                TryMapEventName(item.Name, out var type))
            {
                var content = item.Attributes.GetValue(GenAIHelpers.GenAIEventContent);
                var parts = content != null ? DeserializeBody(type.Value, content) : [];
                viewModel.Items.Add(CreateMessage(viewModel, currentIndex, type.Value, parts, internalId: null));
                currentIndex++;
            }
        }
    }

    private static int ParseMessages(GenAIVisualizerDialogViewModel viewModel, string messages, bool isOutput, ref int currentIndex)
    {
        var inputParts = JsonSerializer.Deserialize(messages, GenAIMessagesContext.Default.ListChatMessage)!;
        foreach (var msg in inputParts)
        {
            var parts = msg.Parts.Select(p => new GenAIItemPartViewModel
            {
                MessagePart = p,
                ErrorMessage = null,
                TextVisualizerViewModel = CreateMessagePartVisualizer(p)
            }).ToList();
            var type = msg.Role switch
            {
                "system" => GenAIItemType.SystemMessage,
                "user" => msg.Parts.All(p => p is ToolCallResponsePart) ? GenAIItemType.ToolMessage : GenAIItemType.UserMessage,
                "assistant" => isOutput ? GenAIItemType.OutputMessage : GenAIItemType.AssistantMessage,
                _ => GenAIItemType.UserMessage
            };
            viewModel.Items.Add(CreateMessage(viewModel, currentIndex, type, parts, internalId: null));
            currentIndex++;
        }

        return currentIndex;
    }

    private static TextVisualizerViewModel CreateMessagePartVisualizer(MessagePart p)
    {
        if (p is TextPart textPart)
        {
            return new TextVisualizerViewModel(textPart.Content ?? string.Empty, indentText: true, fallbackFormat: DashboardUIHelpers.MarkdownFormat);
        }
        if (p is ToolCallRequestPart toolCallRequestPart)
        {
            return new TextVisualizerViewModel($"{toolCallRequestPart.Name}({toolCallRequestPart.Arguments?.ToJsonString()})", indentText: true, knownFormat: DashboardUIHelpers.JavascriptFormat);
        }
        if (p is ToolCallResponsePart toolCallResponsePart)
        {
            return new TextVisualizerViewModel(toolCallResponsePart.Response?.ToJsonString() ?? string.Empty, indentText: true);
        }

        return new TextVisualizerViewModel(string.Empty, indentText: false, knownFormat: DashboardUIHelpers.PlaintextFormat);
    }

    private static GenAIItemViewModel CreateMessage(GenAIVisualizerDialogViewModel viewModel, int currentIndex, GenAIItemType type, List<GenAIItemPartViewModel> parts, long? internalId)
    {
        return new GenAIItemViewModel
        {
            Index = currentIndex,
            InternalId = internalId,
            Type = type,
            Parent = viewModel.Span,
            ResourceName = type is GenAIItemType.AssistantMessage or GenAIItemType.OutputMessage ? viewModel.PeerName! : viewModel.SourceName!,
            ItemParts = parts
        };
    }

    private static List<GenAIItemPartViewModel> DeserializeBody(GenAIItemType type, string message)
    {
        var messagePartViewModels = new List<GenAIItemPartViewModel>();

        switch (type)
        {
            case GenAIItemType.SystemMessage:
            case GenAIItemType.UserMessage:
                var systemOrUserEvent = JsonSerializer.Deserialize(message, GenAIEventsContext.Default.SystemOrUserEvent)!;
                messagePartViewModels.Add(new()
                {
                    MessagePart = new TextPart { Content = systemOrUserEvent.Content },
                    ErrorMessage = null,
                    TextVisualizerViewModel = new TextVisualizerViewModel(systemOrUserEvent.Content ?? string.Empty, indentText: true, fallbackFormat: DashboardUIHelpers.MarkdownFormat)
                });
                break;
            case GenAIItemType.AssistantMessage:
                var assistantEvent = JsonSerializer.Deserialize(message, GenAIEventsContext.Default.AssistantEvent)!;
                ProcessAssistantEvent(messagePartViewModels, assistantEvent);
                break;
            case GenAIItemType.ToolMessage:
                var toolEvent = JsonSerializer.Deserialize(message, GenAIEventsContext.Default.ToolEvent)!;
                var toolResponse = ProcessJsonPayload(toolEvent.Content);
                messagePartViewModels.Add(new()
                {
                    MessagePart = new ToolCallResponsePart { Id = toolEvent.Id, Response = toolResponse },
                    ErrorMessage = null,
                    TextVisualizerViewModel = new TextVisualizerViewModel(toolResponse?.ToJsonString() ?? string.Empty, indentText: true)
                });
                break;
            case GenAIItemType.OutputMessage:
                var choiceEvent = JsonSerializer.Deserialize(message, GenAIEventsContext.Default.ChoiceEvent)!;
                if (choiceEvent.Message is { } m)
                {
                    ProcessAssistantEvent(messagePartViewModels, m);
                }
                break;
        }

        return messagePartViewModels;

        static void ProcessAssistantEvent(List<GenAIItemPartViewModel> messagePartViewModels, AssistantEvent assistantEvent)
        {
            if (assistantEvent.Content != null)
            {
                messagePartViewModels.Add(new()
                {
                    MessagePart = new TextPart { Content = assistantEvent.Content },
                    ErrorMessage = null,
                    TextVisualizerViewModel = new TextVisualizerViewModel(assistantEvent.Content, indentText: true, fallbackFormat: DashboardUIHelpers.MarkdownFormat)
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
                        ErrorMessage = null,
                        TextVisualizerViewModel = new TextVisualizerViewModel(content, indentText: false, knownFormat: "javascript")
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

    private static bool TryMapEventName(string name, [NotNullWhen(true)] out GenAIItemType? type)
    {
        type = name switch
        {
            "gen_ai.system.message" => GenAIItemType.SystemMessage,
            "gen_ai.user.message" => GenAIItemType.UserMessage,
            "gen_ai.assistant.message" => GenAIItemType.AssistantMessage,
            "gen_ai.tool.message" => GenAIItemType.ToolMessage,
            "gen_ai.choice" => GenAIItemType.OutputMessage,
            _ => null
        };

        return type != null;
    }

    private static List<OtlpLogEntry> GetSpanLogEntries(TelemetryRepository telemetryRepository, OtlpSpan span)
    {
        var logsContext = new GetLogsContext
        {
            ResourceKey = null,
            Count = int.MaxValue,
            StartIndex = 0,
            Filters = [
                new FieldTelemetryFilter
                {
                    Field = KnownStructuredLogFields.SpanIdField,
                    Condition = FilterCondition.Equals,
                    Value = span.SpanId
                }
            ]
        };
        var logsResult = telemetryRepository.GetLogs(logsContext);
        return logsResult.Items;
    }
}

public enum OverviewViewKind
{
    InputOutput,
    Details
}

public enum ItemViewKind
{
    Preview,
    Raw,
    Toolcalls
}

public record BadgeDetail(string Text, string Class, Icon Icon);
