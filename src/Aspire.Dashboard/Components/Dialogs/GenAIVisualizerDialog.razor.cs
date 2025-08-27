// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class GenAIVisualizerDialog : ComponentBase
{
    private TreeGenAISelector? _treeGenAISelector;

    [Parameter, EditorRequired]
    public required GenAIVisualizerDialogViewModel Content { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    protected override void OnInitialized()
    {
        foreach (var item in Content.LogEntries)
        {
            if (item.Attributes.GetValue("event.name") is { } name && TryMapEventName(name, out var type))
            {
                Content.Events.Add(new GenAIEvent
                {
                    InternalId = item.InternalId,
                    Type = type.Value,
                    Parent = Content.Span,
                    TimeStamp = item.TimeStamp,
                    Body = DeserializeBody(type.Value, item.Message)
                });
            }
        }

        var resources = TelemetryRepository.GetResources();
        Content.SourceName = OtlpResource.GetResourceName(Content.Span.Source, resources);

        if (TelemetryRepository.GetPeerResource(Content.Span) is { } peerResource)
        {
            Content.PeerName = OtlpResource.GetResourceName(peerResource, resources);
        }
        else
        {
            Content.PeerName = OtlpHelpers.GetPeerAddress(Content.Span.Attributes)!;
        }
    }

    private static object? DeserializeBody(GenAIEventType type, string message)
    {
        return type switch
        {
            GenAIEventType.SystemMessage or GenAIEventType.UserMessage => JsonSerializer.Deserialize(message, OtelContext.Default.SystemOrUserEvent),
            GenAIEventType.AssistantMessage => JsonSerializer.Deserialize(message, OtelContext.Default.AssistantEvent),
            GenAIEventType.ToolMessage => JsonSerializer.Deserialize(message, OtelContext.Default.ToolEvent),
            GenAIEventType.Choice => JsonSerializer.Deserialize(message, OtelContext.Default.ChoiceEvent),
            _ => null
        };
    }

    private static bool TryMapEventName(string name, [NotNullWhen(true)]out GenAIEventType? type)
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

    private Task HandleSelectedTreeItemChangedAsync()
    {
        _ = Content;
        return Task.CompletedTask;
    }

    private static string GetEventTitle(GenAIEvent e)
    {
        return e.Type switch
        {
            GenAIEventType.SystemMessage => "System message",
            GenAIEventType.UserMessage => "User",
            GenAIEventType.AssistantMessage or GenAIEventType.Choice => "Assistant",
            GenAIEventType.ToolMessage => "Tool",
            _ => string.Empty
        };
    }

    public static async Task OpenDialogAsync(ViewportInformation viewportInformation, IDialogService dialogService,
        IStringLocalizer<Resources.Dialogs> dialogsLoc, string title, OtlpSpan span, List<OtlpLogEntry> logEntries)
    {
        var width = viewportInformation.IsDesktop ? "75vw" : "100vw";
        var parameters = new DialogParameters
        {
            Title = title,
            DismissTitle = dialogsLoc[nameof(Resources.Dialogs.DialogCloseButtonText)],
            Width = $"min(1000px, {width})",
            TrapFocus = true,
            Modal = true,
            PreventScroll = true,
        };

        var vm = new GenAIVisualizerDialogViewModel
        {
            Title = title,
            Span = span,
            LogEntries = logEntries
        };

        await dialogService.ShowDialogAsync<GenAIVisualizerDialog>(vm, parameters);
    }
}

