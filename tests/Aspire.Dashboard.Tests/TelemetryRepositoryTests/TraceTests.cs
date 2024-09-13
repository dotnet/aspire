// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class TraceTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(OtlpSpanKind.Server, Span.Types.SpanKind.Server)]
    [InlineData(OtlpSpanKind.Client, Span.Types.SpanKind.Client)]
    [InlineData(OtlpSpanKind.Consumer, Span.Types.SpanKind.Consumer)]
    [InlineData(OtlpSpanKind.Producer, Span.Types.SpanKind.Producer)]
    [InlineData(OtlpSpanKind.Internal, Span.Types.SpanKind.Internal)]
    [InlineData(OtlpSpanKind.Internal, Span.Types.SpanKind.Unspecified)]
    [InlineData(OtlpSpanKind.Unspecified, (Span.Types.SpanKind)1000)]
    public void ConvertSpanKind(OtlpSpanKind expected, Span.Types.SpanKind value)
    {
        var result = TelemetryRepository.ConvertSpanKind(value);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddTraces()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
                Assert.Equal(2, trace.Spans.Count);
            });
    }

    [Fact]
    public void AddTraces_Scope_Multiple()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
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
                        Scope = CreateScope("scope1"),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                        }
                    }
                }
            }
        });
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope("scope2"),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
                Assert.Equal(2, trace.Spans.Count);

                Assert.Collection(trace.Spans,
                    span => Assert.Equal("scope1", span.Scope.ScopeName),
                    span => Assert.Equal("scope2", span.Scope.ScopeName));
            });
    }

    [Fact]
    public void AddTraces_Traces_MultipleOutOrOrder()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext1 = new AddContext();
        repository.AddTraces(addContext1, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext1.FailureCount);

        var addContext2 = new AddContext();
        repository.AddTraces(addContext2, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext2.FailureCount);

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var traces1 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces1.PagedResult.Items,
            trace =>
            {
                AssertId("2", trace.TraceId);
                AssertId("2-1", trace.FirstSpan.SpanId);
                AssertId("2-1", trace.RootSpan!.SpanId);
            },
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-2", trace.FirstSpan.SpanId);
                Assert.Null(trace.RootSpan);
            });

        var addContext3 = new AddContext();
        repository.AddTraces(addContext3, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext3.FailureCount);

        var traces2 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces2.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                Assert.Equal("", trace.FirstSpan.Scope.ScopeName);
                AssertId("1-1", trace.RootSpan!.SpanId);
            },
            trace =>
            {
                AssertId("2", trace.TraceId);
                AssertId("2-1", trace.FirstSpan.SpanId);
                Assert.Equal("", trace.FirstSpan.Scope.ScopeName);
                AssertId("2-1", trace.RootSpan!.SpanId);
            });
    }

    [Fact]
    public void AddTraces_Spans_MultipleOutOrOrder()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>()
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
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-5", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-4", startTime: s_testTime.AddMinutes(4), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
                Assert.Collection(trace.Spans,
                    s => AssertId("1-1", s.SpanId),
                    s => AssertId("1-2", s.SpanId),
                    s => AssertId("1-3", s.SpanId),
                    s => AssertId("1-4", s.SpanId),
                    s => AssertId("1-5", s.SpanId));
            });
    }

    [Fact]
    public void AddTraces_SpanEvents_ReturnData()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>()
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), events: new List<Span.Types.Event>
                            {
                                new Span.Types.Event
                                {
                                    Name = "Event 2",
                                    TimeUnixNano = 2,
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                },
                                new Span.Types.Event
                                {
                                    Name = "Event 1",
                                    TimeUnixNano = 1,
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                }
                            })
                        }
                    }
                }
            }
        });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                Assert.Collection(trace.FirstSpan.Events,
                    e =>
                    {
                        Assert.Equal("Event 1", e.Name);
                        Assert.Collection(e.Attributes,
                            a =>
                            {
                                Assert.Equal("key1", a.Key);
                                Assert.Equal("Value!", a.Value);
                            });
                    },
                    e =>
                    {
                        Assert.Equal("Event 2", e.Name);
                    });
            });
    }

    [Fact]
    public void AddTraces_SpanLinks_ReturnData()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>()
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), links: new List<Span.Types.Link>
                            {
                                new Span.Types.Link
                                {
                                    TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("1")),
                                    SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("1-1")),
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                },
                                new Span.Types.Link
                                {
                                    TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("2")),
                                    SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("2-1")),
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                }
                            })
                        }
                    }
                }
            }
        });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                Assert.Collection(trace.FirstSpan.Links,
                    l =>
                    {
                        AssertId("1", l.TraceId);
                        AssertId("1-1", l.SpanId);
                        Assert.Collection(l.Attributes,
                            a =>
                            {
                                Assert.Equal("key2", a.Key);
                                Assert.Equal("Value!", a.Value);
                            });
                    },
                    l =>
                    {
                        AssertId("2", l.TraceId);
                        AssertId("2-1", l.SpanId);
                        Assert.Collection(l.Attributes,
                            a =>
                            {
                                Assert.Equal("key1", a.Key);
                                Assert.Equal("Value!", a.Value);
                            });
                    });
            });

        Assert.Collection(repository.SpanLinks,
            l =>
            {
                AssertId("1", l.TraceId);
                AssertId("1-1", l.SpanId);
                Assert.Collection(l.Attributes,
                    a =>
                    {
                        Assert.Equal("key2", a.Key);
                        Assert.Equal("Value!", a.Value);
                    });
            },
            l =>
            {
                AssertId("2", l.TraceId);
                AssertId("2-1", l.SpanId);
                Assert.Collection(l.Attributes,
                    a =>
                    {
                        Assert.Equal("key1", a.Key);
                        Assert.Equal("Value!", a.Value);
                    });
            });
    }

    [Fact]
    public void GetTraces_ReturnCopies()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext1 = new AddContext();
        repository.AddTraces(addContext1, new RepeatedField<ResourceSpans>()
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        var traces1 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces1.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
            });

        var traces2 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.NotSame(traces1.PagedResult.Items[0], traces2.PagedResult.Items[0]);
        Assert.NotSame(traces1.PagedResult.Items[0].Spans[0].Trace, traces2.PagedResult.Items[0].Spans[0].Trace);

        var trace1 = repository.GetTrace(GetHexId("1"))!;
        var trace2 = repository.GetTrace(GetHexId("1"))!;
        Assert.NotSame(trace1, trace2);
        Assert.NotSame(trace1.Spans[0].Trace, trace2.Spans[0].Trace);
    }

    [Fact]
    public void AddTraces_AttributeAndEventLimits_LimitsApplied()
    {
        // Arrange
        var repository = CreateRepository(maxAttributeCount: 5, maxAttributeLength: 16, maxSpanEventCount: 5);

        var attributes = new List<KeyValuePair<string, string>>();
        for (var i = 0; i < 10; i++)
        {
            var value = GetValue((i + 1) * 5);
            attributes.Add(new KeyValuePair<string, string>($"Key{i}", value));
        }

        var events = new List<Span.Types.Event>();
        for (var i = 0; i < 10; i++)
        {
            events.Add(CreateSpanEvent($"Event {i}", i, attributes));
        }

        // Act
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes, events: events)
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });

        var trace = Assert.Single(traces.PagedResult.Items);

        AssertId("1", trace.TraceId);
        AssertId("1-1", trace.FirstSpan.SpanId);
        Assert.Collection(trace.FirstSpan.Attributes,
            p =>
            {
                Assert.Equal("Key0", p.Key);
                Assert.Equal("01234", p.Value);
            },
            p =>
            {
                Assert.Equal("Key1", p.Key);
                Assert.Equal("0123456789", p.Value);
            },
            p =>
            {
                Assert.Equal("Key2", p.Key);
                Assert.Equal("012345678901234", p.Value);
            },
            p =>
            {
                Assert.Equal("Key3", p.Key);
                Assert.Equal("0123456789012345", p.Value);
            },
            p =>
            {
                Assert.Equal("Key4", p.Key);
                Assert.Equal("0123456789012345", p.Value);
            });

        Assert.Equal(5, trace.FirstSpan.Events.Count);
        Assert.Equal(5, trace.FirstSpan.Events[0].Attributes.Length);
    }

    [Fact]
    public void AddTraces_Links_BacklinksPopulated()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        AddTrace(repository, "1", s_testTime);
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });

        // Assert
        var trace = Assert.Single(traces.PagedResult.Items);

        Assert.Collection(trace.Spans,
            s =>
            {
                var link = Assert.Single(s.Links);
                AssertId("1-2", link.SpanId);
                AssertId("1-1", link.SourceSpanId);

                var backLink = Assert.Single(s.BackLinks);
                AssertId("1-1", backLink.SpanId);
                AssertId("1-2", backLink.SourceSpanId);
            },
            s =>
            {
                var link = Assert.Single(s.Links);
                AssertId("1-1", link.SpanId);
                AssertId("1-2", link.SourceSpanId);

                var backLink = Assert.Single(s.BackLinks);
                AssertId("1-2", backLink.SpanId);
                AssertId("1-1", backLink.SourceSpanId);
            });
    }

    [Fact]
    public void AddTraces_ExceedLimit_FirstInFirstOut()
    {
        // Arrange
        const int MaxTraceCount = 10;
        var repository = CreateRepository(maxTraceCount: MaxTraceCount);

        var testTime = s_testTime.AddDays(1);

        // Act
        for (var i = 0; i < 2000; i++)
        {
            var traceNumber = i + 1;
            var traceId = traceNumber.ToString(CultureInfo.InvariantCulture);

            // Insert traces out of order to stress the circular buffer type.
            var startTime = testTime.AddMinutes(i + (i % 2 == 0 ? -5 : 0));

            try
            {
                AddTrace(repository, traceId, startTime);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error adding trace number {i}.", ex);
            }
        }

        // Assert
        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });

        // Most recent traces are returned.
        var first = GetStringId(traces.PagedResult.Items.First().TraceId);
        var last = GetStringId(traces.PagedResult.Items.Last().TraceId);
        Assert.Equal("1988", first);
        Assert.Equal("2000", last);

        // Traces returned are ordered by start time.
        var actualOrder = traces.PagedResult.Items.Select(t => t.TraceId).ToList();
        var expectedOrder = traces.PagedResult.Items.OrderBy(t => t.FirstSpan.StartTime).Select(t => t.TraceId).ToList();
        Assert.Equal(expectedOrder, actualOrder);

        Assert.Equal(MaxTraceCount * 2, repository.SpanLinks.Count);
    }

    private static void AddTrace(TelemetryRepository repository, string traceId, DateTime startTime)
    {
        var addContext = new AddContext();

        var link1 = new Span.Types.Link
        {
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(traceId)),
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes($"{traceId}-2")),
            Attributes =
            {
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
            }
        };
        var link2 = new Span.Types.Link
        {
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(traceId)),
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes($"{traceId}-1")),
            Attributes =
            {
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
            }
        };

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
                            CreateSpan(traceId: traceId, spanId: $"{traceId}-2", startTime: startTime.AddMinutes(5), endTime: startTime.AddMinutes(1), parentSpanId: $"{traceId}-1", links: new List<Span.Types.Link>
                            {
                                link2
                            }),
                            CreateSpan(traceId: traceId, spanId: $"{traceId}-1", startTime: startTime.AddMinutes(1), endTime: startTime.AddMinutes(10), links: new List<Span.Types.Link>
                            {
                                link1
                            })
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    [Fact]
    public void AddTraces_MultipleRootSpans_RootSpanIsEarliestWithoutParent()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(4), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-2", trace.FirstSpan.SpanId); // First by time
                AssertId("1-3", trace.RootSpan!.SpanId); // First by time and without a parent
                Assert.Equal(3, trace.Spans.Count);
            });
    }

    [Fact]
    public void GetTraces_MultipleInstances()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)) }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10)) }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(name: "app2"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "3", spanId: "3-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10)) }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var appKey = new ApplicationKey("app1", InstanceId: null);
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = appKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
            },
            trace =>
            {
                AssertId("2", trace.TraceId);
            });
    }

    [Fact]
    public void AddTraces_OutOfOrder_FullName()
    {
        // Arrange
        var repository = CreateRepository();
        var request = new GetTracesRequest
        {
            ApplicationKey = new ApplicationKey("TestService", "TestId"),
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        };

        // Act 1
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
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(10), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        // Assert 1
        var trace = Assert.Single(repository.GetTraces(request).PagedResult.Items);
        Assert.Equal("TestService: Test span. Id: 1-3", trace.FullName);

        // Act 2
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
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        // Assert 2
        trace = Assert.Single(repository.GetTraces(request).PagedResult.Items);
        Assert.Equal("TestService: Test span. Id: 1-2", trace.FullName);

        // Act 3
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
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(10), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        // Assert 3
        trace = Assert.Single(repository.GetTraces(request).PagedResult.Items);
        Assert.Equal("TestService: Test span. Id: 1-1", trace.FullName);

        // Act 4
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
                            CreateSpan(traceId: "1", spanId: "1-4", startTime: s_testTime, endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.Equal(0, addContext.FailureCount);

        // Assert 4
        trace = Assert.Single(repository.GetTraces(request).PagedResult.Items);
        Assert.Equal("TestService: Test span. Id: 1-1", trace.FullName);
    }

    [Fact]
    public void AddTraces_SameResourceDifferentProperties_MultipleResourceViews()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(attributes: [KeyValuePair.Create("prop1", "value1")]),
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
            },
            new ResourceSpans
            {
                Resource = CreateResource(attributes: [KeyValuePair.Create("prop2", "value1"), KeyValuePair.Create("prop1", "value2")]),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(attributes: [KeyValuePair.Create("prop1", "value2"), KeyValuePair.Create("prop2", "value1")]),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(10), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        // Spans belong to the same application
        var application = Assert.Single(repository.GetApplications());
        Assert.Equal("TestService", application.ApplicationName);
        Assert.Equal("TestId", application.InstanceId);

        // Spans have different views
        var views = application.GetViews().OrderBy(v => v.Properties.Length).ToList();
        Assert.Collection(views,
            v =>
            {
                Assert.Collection(v.Properties,
                    p =>
                    {
                        Assert.Equal("prop1", p.Key);
                        Assert.Equal("value1", p.Value);
                    });
            },
            v =>
            {
                Assert.Collection(v.Properties,
                    p =>
                    {
                        Assert.Equal("prop1", p.Key);
                        Assert.Equal("value2", p.Value);
                    },
                    p =>
                    {
                        Assert.Equal("prop2", p.Key);
                        Assert.Equal("value1", p.Value);
                    });
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = application.ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10
        });
        var trace = Assert.Single(traces.PagedResult.Items);

        Assert.Collection(trace.Spans,
            s =>
            {
                AssertId("1-1", s.SpanId);
                Assert.Collection(s.Source.Properties,
                    p =>
                    {
                        Assert.Equal("prop1", p.Key);
                        Assert.Equal("value1", p.Value);
                    });
            },
            s =>
            {
                AssertId("1-2", s.SpanId);
                Assert.Collection(s.Source.Properties,
                    p =>
                    {
                        Assert.Equal("prop1", p.Key);
                        Assert.Equal("value2", p.Value);
                    },
                    p =>
                    {
                        Assert.Equal("prop2", p.Key);
                        Assert.Equal("value1", p.Value);
                    });
            },
            s =>
            {
                AssertId("1-3", s.SpanId);
                Assert.Collection(s.Source.Properties,
                    p =>
                    {
                        Assert.Equal("prop1", p.Key);
                        Assert.Equal("value2", p.Value);
                    },
                    p =>
                    {
                        Assert.Equal("prop2", p.Key);
                        Assert.Equal("value1", p.Value);
                    });
            });
    }
}
