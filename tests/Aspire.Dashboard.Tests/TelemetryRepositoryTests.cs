// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class TelemetryRepositoryTests
{
    [Fact]
    public void AddLogs()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        LogRecords = { CreateLogRecord() }
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

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationServiceId = applications[0].InstanceId,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.Collection(logs.Items,
            app =>
            {
                Assert.Equal("546573745370616e4964", app.SpanId);
                Assert.Equal("5465737454726163654964", app.TraceId);
                Assert.Equal("TestParentId", app.ParentId);
                Assert.Equal("Test {Log}", app.OriginalFormat);
                Assert.Equal("Test Value!", app.Message);
                Assert.Collection(app.Properties,
                    p =>
                    {
                        Assert.Equal("Log", p.Key);
                        Assert.Equal("Value!", p.Value);
                    });
            });

        var propertyKeys = repository.GetLogPropertyKeys(applications[0].InstanceId)!;
        Assert.Collection(propertyKeys,
            s => Assert.Equal("Log", s));
    }

    // Test is disabled as it is currently broken. Issue tracking the fix is: https://github.com/dotnet/astra/issues/674
    // When uncommenting next line, test needs to be made public.
    // [Fact]
    private void GetLogs_UnknownApplication()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationServiceId = "UnknownApplication",
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        // Assert
        Assert.Empty(logs.Items);
    }

    [Fact]
    public void GetLogPropertyKeys_UnknownApplication()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var propertyKeys = repository.GetLogPropertyKeys("UnknownApplication");

        // Assert
        Assert.Empty(propertyKeys);
    }

    [Fact]
    public async Task Subscriptions_AddLog()
    {
        // Arrange
        var repository = CreateRepository();

        var newApplicationsTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.OnNewApplications(() =>
        {
            newApplicationsTcs.TrySetResult();
            return Task.CompletedTask;
        });

        // Act 1
        var addContext1 = new AddContext();
        repository.AddLogs(addContext1, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert 1
        Assert.Equal(0, addContext1.FailureCount);
        await newApplicationsTcs.Task;

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        // Act 2
        var newLogsTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.OnNewLogs(applications[0].InstanceId, () =>
        {
            newLogsTcs.TrySetResult();
            return Task.CompletedTask;
        });

        var addContext2 = new AddContext();
        repository.AddLogs(addContext2, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        await newLogsTcs.Task;

        // Assert 2
        Assert.Equal(0, addContext2.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationServiceId = applications[0].InstanceId,
            StartIndex = 0,
            Count = 1,
            Filters = []
        })!;
        Assert.Single(logs.Items);
        Assert.Equal(2, logs.TotalItemCount);
    }

    [Fact]
    public void Unsubscribe()
    {
        // Arrange
        var repository = CreateRepository();

        var onNewApplicationsCalled = false;
        var subscription = repository.OnNewApplications(() =>
        {
            onNewApplicationsCalled = true;
            return Task.CompletedTask;
        });
        subscription.Dispose();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);
        Assert.False(onNewApplicationsCalled, "Callback shouldn't have been called because subscription was disposed.");
    }

    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        var traces = repository.GetTraces(new GetTracesContext
        {
            ApplicationServiceId = applications[0].InstanceId,
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
    public void AddTraces_MultipleOutOrOrder()
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
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });

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
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var traces1 = repository.GetTraces(new GetTracesContext
        {
            ApplicationServiceId = applications[0].InstanceId,
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
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        var traces2 = repository.GetTraces(new GetTracesContext
        {
            ApplicationServiceId = applications[0].InstanceId,
            StartIndex = 0,
            Count = 10
        });
        Assert.Collection(traces2.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
            },
            trace =>
            {
                AssertId("2", trace.TraceId);
                AssertId("2-1", trace.FirstSpan.SpanId);
                AssertId("2-1", trace.RootSpan!.SpanId);
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

        var traces1 = repository.GetTraces(new GetTracesContext
        {
            ApplicationServiceId = null,
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

        var traces2 = repository.GetTraces(new GetTracesContext
        {
            ApplicationServiceId = null,
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

    private static void AssertId(string expected, string actual)
    {
        var bytes = Convert.FromHexString(actual);
        var resolvedActual = Encoding.UTF8.GetString(bytes);

        Assert.Equal(expected, resolvedActual);
    }

    private static string GetHexId(string text)
    {
        var id = Encoding.UTF8.GetBytes(text);
        return OtlpHelpers.ToHexString(id);
    }

    private static InstrumentationScope CreateScope()
    {
        return new InstrumentationScope() { Name = "TestScope" };
    }

    private static Span CreateSpan(string traceId, string spanId, DateTime startTime, DateTime endTime, string? parentSpanId = null)
    {
        return new Span
        {
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(traceId)),
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(spanId)),
            ParentSpanId = parentSpanId is null ? ByteString.Empty : ByteString.CopyFrom(Encoding.UTF8.GetBytes(parentSpanId)),
            StartTimeUnixNano = DateTimeToUnixNanoseconds(startTime),
            EndTimeUnixNano = DateTimeToUnixNanoseconds(endTime),
            Name = "Test span"
        };
    }

    private static LogRecord CreateLogRecord()
    {
        return new LogRecord
        {
            Body = new AnyValue { StringValue = "Test Value!" },
            TraceId = ByteString.CopyFrom(Convert.FromHexString("5465737454726163654964")),
            SpanId = ByteString.CopyFrom(Convert.FromHexString("546573745370616e4964")),
            TimeUnixNano = 1000,
            SeverityNumber = SeverityNumber.Info,
            Attributes =
            {
                new KeyValue { Key = "{OriginalFormat}", Value = new AnyValue { StringValue = "Test {Log}" } },
                new KeyValue { Key = "ParentId", Value = new AnyValue { StringValue = "TestParentId" } },
                new KeyValue { Key = "Log", Value = new AnyValue { StringValue = "Value!" } }
            }
        };
    }

    private static Resource CreateResource()
    {
        return new Resource()
        {
            Attributes =
            {
                new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "TestService" } },
                new KeyValue { Key = "service.instance.id", Value = new AnyValue { StringValue = "TestId" } }
            }
        };
    }

    private static TelemetryRepository CreateRepository()
    {
        return new TelemetryRepository(new ConfigurationManager(), NullLoggerFactory.Instance);
    }

    private static ulong DateTimeToUnixNanoseconds(DateTime dateTime)
    {
        DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan timeSinceEpoch = dateTime.ToUniversalTime() - unixEpoch;

        return (ulong)timeSinceEpoch.Ticks / 100;
    }
}
