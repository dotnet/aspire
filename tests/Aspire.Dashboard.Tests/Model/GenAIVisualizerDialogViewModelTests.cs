// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.GenAI;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.Model;

public sealed class GenAIVisualizerDialogViewModelTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_NoGenAIAttributes_NoMessages()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel, []);

        // Assert
        Assert.Empty(vm.Messages);
        Assert.Null(vm.SelectedMessage);
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
    }

    [Fact]
    public void Create_GenAILogEntries_HasMessages()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create("server.address", "ai-server.address")])
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope(),
                        LogRecords =
                        {
                            CreateLogRecord(
                                time: s_testTime,
                                traceId: "1",
                                spanId: "1-1",
                                message: JsonSerializer.Serialize(new SystemOrUserEvent { Content = "System!" }, OtelContext.Default.SystemOrUserEvent),
                                attributes: [KeyValuePair.Create("event.name", "gen_ai.system.message")]),
                            CreateLogRecord(
                                time: s_testTime.AddSeconds(1),
                                traceId: "1",
                                spanId: "1-1",
                                message: JsonSerializer.Serialize(new SystemOrUserEvent { Content = "User!" }, OtelContext.Default.SystemOrUserEvent),
                                attributes: [KeyValuePair.Create("event.name", "gen_ai.user.message")]),
                            CreateLogRecord(
                                time: s_testTime.AddSeconds(2),
                                traceId: "1",
                                spanId: "1-1",
                                message: JsonSerializer.Serialize(new AssistantEvent { Content = "Assistant!" }, OtelContext.Default.AssistantEvent),
                                attributes: [KeyValuePair.Create("event.name", "gen_ai.assistant.message")])
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(
            repository,
            spanDetailsViewModel,
            repository.GetLogs(new GetLogsContext { StartIndex = 0, Count = int.MaxValue, ResourceKey = null, Filters = [] }).Items);

        // Assert
        Assert.Collection(vm.Messages,
            m =>
            {
                Assert.Equal(GenAIEventType.SystemMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.MessageParts,
                    p => Assert.Equal("System!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIEventType.UserMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.MessageParts,
                    p => Assert.Equal("User!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIEventType.AssistantMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.MessageParts,
                    p => Assert.Equal("Assistant!", Assert.IsType<TextPart>(p.MessagePart).Content));
            });
        Assert.Null(vm.SelectedMessage);
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
    }

    private static GenAIVisualizerDialogViewModel Create(
        TelemetryRepository repository,
        SpanDetailsViewModel spanDetailsViewModel,
        List<OtlpLogEntry> logEntries)
    {
        return GenAIVisualizerDialogViewModel.Create(
            logEntries,
            spanDetailsViewModel,
            selectedLogEntryId: null,
            telemetryRepository: repository);
    }
}
