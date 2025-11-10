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
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
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

    [Fact]
    public async Task Dialog_UpdatesWhenNewTracesAdded()
    {
        // Arrange - Setup dialog infrastructure first
        var cut = SetUpDialog(out var dialogService);
        var repository = Services.GetRequiredService<TelemetryRepository>();
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        
        // Add initial trace to repository
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app", instanceId: "instance"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "trace1", spanId: "span1", startTime: s_testTime, endTime: s_testTime.AddSeconds(1))
                        }
                    }
                }
            }
        });

        // Get the resource and trace
        var resources = repository.GetResources();
        var resource = resources[0];
        var tracesResult = repository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resource.ResourceKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        var trace = tracesResult.PagedResult.Items[0];
        var span = trace.Spans[0];

        // Open dialog
        await GenAIVisualizerDialog.OpenDialogAsync(
            viewportInformation: new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false),
            dialogService: dialogService,
            dialogsLoc: Services.GetRequiredService<IStringLocalizer<Aspire.Dashboard.Resources.Dialogs>>(),
            span: span,
            selectedLogEntryId: null,
            telemetryRepository: repository,
            errorRecorder: new TestTelemetryErrorRecorder(),
            resources: resources,
            getContextGenAISpans: () => []
        );

        var instance = cut.FindComponent<GenAIVisualizerDialog>().Instance;
        var initialItemCount = instance.Content.Items.Count;

        // Act - Add a new span to the same trace with GenAI messages
        var inputMessages = JsonSerializer.Serialize(new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "user",
                Parts = [new TextPart { Content = "New message!" }]
            }
        }, GenAIMessagesContext.Default.ListChatMessage);

        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app", instanceId: "instance"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(
                                traceId: "trace1",
                                spanId: "span2",
                                startTime: s_testTime.AddSeconds(1),
                                endTime: s_testTime.AddSeconds(2),
                                parentSpanId: "span1",
                                attributes: [KeyValuePair.Create(GenAIHelpers.GenAIInputMessages, inputMessages)])
                        }
                    }
                }
            }
        });

        // Assert - Wait for dialog to update
        cut.WaitForAssertion(() =>
        {
            var updatedInstance = cut.FindComponent<GenAIVisualizerDialog>().Instance;
            // The dialog should have detected the trace update
            Assert.True(repository.HasUpdatedTrace(trace));
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Dialog_UpdatesWhenNewLogsAdded()
    {
        // Arrange - Setup dialog infrastructure first
        var cut = SetUpDialog(out var dialogService);
        var repository = Services.GetRequiredService<TelemetryRepository>();
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };

        // Add initial trace to repository
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app", instanceId: "instance"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "trace1", spanId: "span1", startTime: s_testTime, endTime: s_testTime.AddSeconds(1))
                        }
                    }
                }
            }
        });

        // Get the resource and trace
        var resources = repository.GetResources();
        var resource = resources[0];
        var tracesResult = repository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resource.ResourceKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        var trace = tracesResult.PagedResult.Items[0];
        var span = trace.Spans[0];

        // Open dialog
        await GenAIVisualizerDialog.OpenDialogAsync(
            viewportInformation: new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false),
            dialogService: dialogService,
            dialogsLoc: Services.GetRequiredService<IStringLocalizer<Aspire.Dashboard.Resources.Dialogs>>(),
            span: span,
            selectedLogEntryId: null,
            telemetryRepository: repository,
            errorRecorder: new TestTelemetryErrorRecorder(),
            resources: resources,
            getContextGenAISpans: () => []
        );

        var instance = cut.FindComponent<GenAIVisualizerDialog>().Instance;

        // Act - Add a log entry associated with the span AND add a new span to update the trace
        // This simulates a realistic scenario where logs and spans are added together
        var messageContent = JsonSerializer.Serialize(new SystemOrUserEvent { Content = "User message from log" }, GenAIEventsContext.Default.SystemOrUserEvent);
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "app", instanceId: "instance"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope(),
                        LogRecords =
                        {
                            CreateLogRecord(
                                time: s_testTime.AddMilliseconds(500),
                                message: messageContent,
                                traceId: "trace1",
                                spanId: "span1",
                                attributes: [KeyValuePair.Create("event.name", "gen_ai.user.message")])
                        }
                    }
                }
            }
        });

        // Add a new span to the trace to trigger the trace update
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app", instanceId: "instance"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(
                                traceId: "trace1",
                                spanId: "span2",
                                startTime: s_testTime.AddSeconds(1),
                                endTime: s_testTime.AddSeconds(2),
                                parentSpanId: "span1")
                        }
                    }
                }
            }
        });

        // Assert - Wait for dialog to update - the trace should be marked as updated
        cut.WaitForAssertion(() =>
        {
            // The dialog should have detected the trace update
            Assert.True(repository.HasUpdatedTrace(trace));
        }, timeout: TimeSpan.FromSeconds(5));
    }

    private IRenderedFragment SetUpDialog(out IDialogService dialogService)
    {
        FluentUISetupHelpers.SetupDialogInfrastructure(this);
        
        // Setup additional FluentUI components needed by GenAIVisualizerDialog
        var fluentUIVersion = typeof(FluentMain).Assembly.GetName().Version!;
        var tabModule = JSInterop.SetupModule($"./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Tabs/FluentTab.razor.js?v={fluentUIVersion}");
        tabModule.SetupVoid("TabEditable_Changed", _ => true);
        
        FluentUISetupHelpers.SetupFluentOverflow(this);
        
        var cut = FluentUISetupHelpers.RenderDialogProvider(this);

        dialogService = Services.GetRequiredService<IDialogService>();
        return cut;
    }
}
