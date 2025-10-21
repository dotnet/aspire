// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Dashboard.Components.Dialogs;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model.GenAI;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Components.Tests.Controls;

public class GenAIVisualizerDialogTests : DashboardTestContext
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Render_NoGenAIAttributes_Success()
    {
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var resource = new OtlpResource("app", "instance", uninstrumentedPeer: false, context);

        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = CreateOtlpScope(context);

        var cut = SetUpDialog(out var dialogService);
        await GenAIVisualizerDialog.OpenDialogAsync(
            viewportInformation: new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false),
            dialogService: dialogService,
            dialogsLoc: Services.GetRequiredService<IStringLocalizer<Aspire.Dashboard.Resources.Dialogs>>(),
            span: CreateOtlpSpan(resource, trace, scope, spanId: "abc", parentSpanId: null, startDate: s_testTime),
            selectedLogEntryId: null,
            telemetryRepository: Services.GetRequiredService<TelemetryRepository>(),
            errorRecorder: new TestTelemetryErrorRecorder(),
            resources: [],
            getContextGenAISpans: () => []
            );

        var instance = cut.FindComponent<GenAIVisualizerDialog>().Instance;

        Assert.Null(instance.Content.DisplayErrorMessage);
        Assert.Empty(instance.Content.Items);
        Assert.Equal("app", instance.Content.SourceName);
        Assert.Equal("unknown-peer", instance.Content.PeerName);
    }

    [Fact]
    public async Task Render_HasGenAIMessages_Success()
    {
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var resource = new OtlpResource("app", "instance", uninstrumentedPeer: false, context);

        var systemInstruction = JsonSerializer.Serialize(new List<MessagePart>
        {
            new TextPart { Content = "System!" }
        }, GenAIMessagesContext.Default.ListMessagePart);

        var inputMessages = JsonSerializer.Serialize(new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "user",
                Parts = [new TextPart { Content = "User!" }]
            },
            new ChatMessage
            {
                Role = "assistant",
                Parts = [new ToolCallRequestPart { Name = "generate_names", Arguments = JsonNode.Parse(@"{""count"":2}") }]
            },
            new ChatMessage
            {
                Role = "user",
                Parts = [new ToolCallResponsePart { Response = JsonNode.Parse(@"[""Jack"",""Jane""]") }]
            }
        }, GenAIMessagesContext.Default.ListChatMessage);

        var outputMessages = JsonSerializer.Serialize(new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "assistant",
                Parts = [new TextPart { Content = "Output!" }]
            }
        }, GenAIMessagesContext.Default.ListChatMessage);

        var trace = new OtlpTrace(new byte[] { 1, 2, 3 }, DateTime.MinValue);
        var scope = CreateOtlpScope(context);
        var span = CreateOtlpSpan(resource, trace, scope, spanId: "abc", parentSpanId: null, startDate: s_testTime, attributes: [
            KeyValuePair.Create(GenAIHelpers.GenAISystemInstructions, systemInstruction),
            KeyValuePair.Create(GenAIHelpers.GenAIInputMessages, inputMessages),
            KeyValuePair.Create(GenAIHelpers.GenAIOutputInstructions, outputMessages)
        ]);

        var cut = SetUpDialog(out var dialogService);
        await GenAIVisualizerDialog.OpenDialogAsync(
            viewportInformation: new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false),
            dialogService: dialogService,
            dialogsLoc: Services.GetRequiredService<IStringLocalizer<Aspire.Dashboard.Resources.Dialogs>>(),
            span: span,
            selectedLogEntryId: null,
            telemetryRepository: Services.GetRequiredService<TelemetryRepository>(),
            errorRecorder: new TestTelemetryErrorRecorder(),
            resources: [],
            getContextGenAISpans: () => []
            );

        var instance = cut.FindComponent<GenAIVisualizerDialog>().Instance;

        Assert.Equal(5, instance.Content.Items.Count);
    }

    private IRenderedFragment SetUpDialog(out IDialogService dialogService)
    {
        FluentUISetupHelpers.SetupDialogInfrastructure(this);
        var cut = FluentUISetupHelpers.RenderDialogProvider(this);

        dialogService = Services.GetRequiredService<IDialogService>();
        return cut;
    }
}
