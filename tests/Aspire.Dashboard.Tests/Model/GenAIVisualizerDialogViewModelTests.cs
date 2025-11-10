// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
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
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Empty(vm.Items);
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
    }

    [Fact]
    public void Create_SpanError_HasErrorItem()
    {
        // Arrange
        var repository = CreateRepository();

        var status = new Status
        {
            Code = Status.Types.StatusCode.Error,
            Message = "Error!"
        };
        var attributes = new KeyValuePair<string, string>[]
        {
            KeyValuePair.Create("error.type", "Exception")
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes, status: status)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Collection(vm.Items,
            i =>
            {
                Assert.Equal(GenAIItemType.Error, i.Type);
                Assert.Collection(i.ItemParts,
                    p => Assert.Equal("""
                        Exception

                        Error!
                        """, p.ErrorMessage));
            });
        Assert.False(vm.NoMessageContent);
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"), KeyValuePair.Create("server.address", "ai-server.address")])
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
                                message: JsonSerializer.Serialize(new SystemOrUserEvent { Content = "System!" }, GenAIEventsContext.Default.SystemOrUserEvent),
                                attributes: [KeyValuePair.Create("event.name", "gen_ai.system.message")]),
                            CreateLogRecord(
                                time: s_testTime.AddSeconds(1),
                                traceId: "1",
                                spanId: "1-1",
                                message: JsonSerializer.Serialize(new SystemOrUserEvent { Content = "User!" }, GenAIEventsContext.Default.SystemOrUserEvent),
                                attributes: [KeyValuePair.Create("event.name", "gen_ai.user.message")]),
                            CreateLogRecord(
                                time: s_testTime.AddSeconds(2),
                                traceId: "1",
                                spanId: "1-1",
                                message: JsonSerializer.Serialize(new AssistantEvent { Content = "Assistant!" }, GenAIEventsContext.Default.AssistantEvent),
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
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Collection(vm.Items,
            m =>
            {
                Assert.Equal(GenAIItemType.SystemMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("System!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.UserMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("User!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.AssistantMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("Assistant!", Assert.IsType<TextPart>(p.MessagePart).Content));
            });
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
    }

    [Fact]
    public void Create_GenAISpanEvents_HasMessages()
    {
        // Arrange
        var repository = CreateRepository();

        var events = new List<Span.Types.Event>
        {
            CreateSpanEvent(
                name: "gen_ai.system.message",
                startTime: 0,
                attributes: [
                    KeyValuePair.Create(GenAIHelpers.GenAIEventContent, JsonSerializer.Serialize(new SystemOrUserEvent { Content = "System!" }, GenAIEventsContext.Default.SystemOrUserEvent)),
                    KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
                ]),
            CreateSpanEvent(
                name: "gen_ai.user.message",
                startTime: 1,
                attributes: [
                    KeyValuePair.Create(GenAIHelpers.GenAIEventContent, JsonSerializer.Serialize(new SystemOrUserEvent { Content = "User!" }, GenAIEventsContext.Default.SystemOrUserEvent)),
                    KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
                ]),
            CreateSpanEvent(
                name: "gen_ai.assistant.message",
                startTime: 2,
                attributes: [
                    KeyValuePair.Create(GenAIHelpers.GenAIEventContent, JsonSerializer.Serialize(new AssistantEvent { Content = "Assistant!" }, GenAIEventsContext.Default.AssistantEvent)),
                ]),
            CreateSpanEvent(
                name: "other_name_that_is_ignored",
                startTime: 3,
                attributes: [
                    KeyValuePair.Create(GenAIHelpers.GenAIEventContent, JsonSerializer.Serialize(new AssistantEvent { Content = "Assistant!" }, GenAIEventsContext.Default.AssistantEvent)),
                    KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
                ])
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"), KeyValuePair.Create("server.address", "ai-server.address")], events: events)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Collection(vm.Items,
            m =>
            {
                Assert.Equal(GenAIItemType.SystemMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("System!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.UserMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("User!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.AssistantMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("Assistant!", Assert.IsType<TextPart>(p.MessagePart).Content));
            });
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
    }

    [Fact]
    public void Create_GenAISpanAttributes_HasMessages()
    {
        // Arrange
        var repository = CreateRepository();

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

        var attributes = new KeyValuePair<string, string>[]
        {
            KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
            KeyValuePair.Create("server.address", "ai-server.address"),
            KeyValuePair.Create(GenAIHelpers.GenAISystemInstructions, systemInstruction),
            KeyValuePair.Create(GenAIHelpers.GenAIInputMessages, inputMessages),
            KeyValuePair.Create(GenAIHelpers.GenAIOutputInstructions, outputMessages)
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Collection(vm.Items,
            m =>
            {
                Assert.Equal(GenAIItemType.SystemMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("System!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.UserMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("User!", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.AssistantMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p =>
                    {
                        var toolCallRequestPart = Assert.IsType<ToolCallRequestPart>(p.MessagePart);
                        Assert.Equal("generate_names", toolCallRequestPart.Name);
                        Assert.Equal(@"{""count"":2}", toolCallRequestPart.Arguments!.ToJsonString());
                    });
            },
            m =>
            {
                Assert.Equal(GenAIItemType.ToolMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal(@"[""Jack"",""Jane""]", Assert.IsType<ToolCallResponsePart>(p.MessagePart).Response!.ToJsonString()));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.OutputMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("Output!", Assert.IsType<TextPart>(p.MessagePart).Content));
            });
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
        Assert.Null(vm.DisplayErrorMessage);
    }

    [Fact]
    public void Create_GenAISpanAttributes_InvalidJson_DisplayErrorMessage()
    {
        // Arrange
        var repository = CreateRepository();

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

        var outputMessages = "invalid!";

        var attributes = new KeyValuePair<string, string>[]
        {
            KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
            KeyValuePair.Create("server.address", "ai-server.address"),
            KeyValuePair.Create(GenAIHelpers.GenAISystemInstructions, systemInstruction),
            KeyValuePair.Create(GenAIHelpers.GenAIInputMessages, inputMessages),
            KeyValuePair.Create(GenAIHelpers.GenAIOutputInstructions, outputMessages)
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Empty(vm.Items);
        Assert.StartsWith("System.InvalidOperationException: ", vm.DisplayErrorMessage);
    }

    [Fact]
    public void Create_GenAISpanAttributesWithoutContent_HasNoMessageContent()
    {
        // Arrange
        var repository = CreateRepository();

        var systemInstruction = JsonSerializer.Serialize(new List<MessagePart>
        {
            new TextPart { Content = "" }
        }, GenAIMessagesContext.Default.ListMessagePart);

        var inputMessages = JsonSerializer.Serialize(new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "user",
                Parts = [new TextPart { Content = "" }]
            },
            new ChatMessage
            {
                Role = "assistant",
                Parts = [new ToolCallRequestPart { Name = "generate_names" }]
            },
            new ChatMessage
            {
                Role = "user",
                Parts = [new ToolCallResponsePart()]
            }
        }, GenAIMessagesContext.Default.ListChatMessage);

        var outputMessages = JsonSerializer.Serialize(new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "assistant",
                Parts = [new TextPart { Content = "" }]
            }
        }, GenAIMessagesContext.Default.ListChatMessage);

        var attributes = new KeyValuePair<string, string>[]
        {
            KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
            KeyValuePair.Create("server.address", "ai-server.address"),
            KeyValuePair.Create(GenAIHelpers.GenAISystemInstructions, systemInstruction),
            KeyValuePair.Create(GenAIHelpers.GenAIInputMessages, inputMessages),
            KeyValuePair.Create(GenAIHelpers.GenAIOutputInstructions, outputMessages)
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Collection(vm.Items,
            m =>
            {
                Assert.Equal(GenAIItemType.SystemMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.UserMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.AssistantMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p =>
                    {
                        var toolCallRequestPart = Assert.IsType<ToolCallRequestPart>(p.MessagePart);
                        Assert.Equal("generate_names", toolCallRequestPart.Name);
                        Assert.Null(toolCallRequestPart.Arguments);
                    });
            },
            m =>
            {
                Assert.Equal(GenAIItemType.ToolMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Null(Assert.IsType<ToolCallResponsePart>(p.MessagePart).Response));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.OutputMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("", Assert.IsType<TextPart>(p.MessagePart).Content));
            });
        Assert.True(vm.NoMessageContent);
    }

    [Fact]
    public void Create_NoMessages_HasNoMessageContent()
    {
        // Arrange
        var repository = CreateRepository();

        var attributes = new KeyValuePair<string, string>[]
        {
            KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
            KeyValuePair.Create("server.address", "ai-server.address"),
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Empty(vm.Items);
        Assert.True(vm.NoMessageContent);
    }

    [Fact]
    public void Create_LangSmithFormat_HasMessages()
    {
        // Arrange
        var repository = CreateRepository();

        // LangSmith uses flattened attributes with indexed format
        var attributes = new KeyValuePair<string, string>[]
        {
            KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
            KeyValuePair.Create("server.address", "ai-server.address"),
            // Prompt messages
            KeyValuePair.Create("gen_ai.prompt.0.role", "system"),
            KeyValuePair.Create("gen_ai.prompt.0.content", "You are a helpful assistant."),
            KeyValuePair.Create("gen_ai.prompt.1.role", "user"),
            KeyValuePair.Create("gen_ai.prompt.1.content", "Hello, how are you?"),
            // Completion messages
            KeyValuePair.Create("gen_ai.completion.0.role", "assistant"),
            KeyValuePair.Create("gen_ai.completion.0.content", "I'm doing well, thank you!")
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Collection(vm.Items,
            m =>
            {
                Assert.Equal(GenAIItemType.SystemMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("You are a helpful assistant.", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.UserMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("Hello, how are you?", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.OutputMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("I'm doing well, thank you!", Assert.IsType<TextPart>(p.MessagePart).Content));
            });
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
    }

    [Fact]
    public void Create_LangSmithFormat_MessageRoleContentFallback_HasMessages()
    {
        // Arrange
        var repository = CreateRepository();

        // LangSmith format with message.role and message.content as fallback
        var attributes = new KeyValuePair<string, string>[]
        {
            KeyValuePair.Create(GenAIHelpers.GenAISystem, "System!"),
            KeyValuePair.Create("server.address", "ai-server.address"),
            // Prompt messages using message.role and message.content
            KeyValuePair.Create("gen_ai.prompt.0.message.role", "system"),
            KeyValuePair.Create("gen_ai.prompt.0.message.content", "You are a coding assistant."),
            KeyValuePair.Create("gen_ai.prompt.1.message.role", "user"),
            KeyValuePair.Create("gen_ai.prompt.1.message.content", "Write a hello world program."),
            // Completion messages using message.role and message.content
            KeyValuePair.Create("gen_ai.completion.0.message.role", "assistant"),
            KeyValuePair.Create("gen_ai.completion.0.message.content", "Here's a simple hello world program...")
        };

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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes)
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        var span = repository.GetSpan(GetHexId("1"), GetHexId("1-1"))!;
        var spanDetailsViewModel = SpanDetailsViewModel.Create(span, repository, repository.GetResources());

        // Act
        var vm = Create(repository, spanDetailsViewModel);

        // Assert
        Assert.Collection(vm.Items,
            m =>
            {
                Assert.Equal(GenAIItemType.SystemMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("You are a coding assistant.", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.UserMessage, m.Type);
                Assert.Equal("TestService", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("Write a hello world program.", Assert.IsType<TextPart>(p.MessagePart).Content));
            },
            m =>
            {
                Assert.Equal(GenAIItemType.OutputMessage, m.Type);
                Assert.Equal("ai-server.address", m.ResourceName);
                Assert.Collection(m.ItemParts,
                    p => Assert.Equal("Here's a simple hello world program...", Assert.IsType<TextPart>(p.MessagePart).Content));
            });
        Assert.Null(vm.ModelName);
        Assert.Null(vm.InputTokens);
        Assert.Null(vm.OutputTokens);
    }

    private static GenAIVisualizerDialogViewModel Create(
        TelemetryRepository repository,
        SpanDetailsViewModel spanDetailsViewModel)
    {
        return GenAIVisualizerDialogViewModel.Create(
            spanDetailsViewModel,
            selectedLogEntryId: null,
            errorRecorder: new TestTelemetryErrorRecorder(),
            telemetryRepository: repository,
            () => [spanDetailsViewModel.Span]);
    }
}
