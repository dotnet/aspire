// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
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
        Assert.Null(logs);
    }

    [Fact]
    public void GetLogPropertyKeys_UnknownApplication()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var propertyKeys = repository.GetLogPropertyKeys("UnknownApplication");

        // Assert
        Assert.Null(propertyKeys);
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
}
