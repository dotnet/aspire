// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Logs.V1;
using Xunit;
using static Aspire.Dashboard.Tests.TelemetryRepositoryTests.TestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class LogTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
                        Scope = CreateScope("TestLogger"),
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
                Assert.Equal("Test {Log}", app.OriginalFormat);
                Assert.Equal("Test Value!", app.Message);
                Assert.Equal("TestLogger", app.Scope.ScopeName);
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

    [Fact]
    public void AddLogs_MultipleOutOfOrder()
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
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1"),
                            CreateLogRecord(time: s_testTime.AddMinutes(2), message: "2"),
                            CreateLogRecord(time: s_testTime.AddMinutes(3), message: "3"),
                            CreateLogRecord(time: s_testTime.AddMinutes(10), message: "10"),
                            CreateLogRecord(time: s_testTime.AddMinutes(9), message: "9"),
                            CreateLogRecord(time: s_testTime.AddMinutes(4), message: "4"),
                            CreateLogRecord(time: s_testTime.AddMinutes(5), message: "5"),
                            CreateLogRecord(time: s_testTime.AddMinutes(7), message: "7"),
                            CreateLogRecord(time: s_testTime.AddMinutes(6), message: "6"),
                            CreateLogRecord(time: s_testTime.AddMinutes(8), message: "8"),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationServiceId = null,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.Collection(logs.Items,
            l =>
            {
                Assert.Equal("1", l.Message);
                Assert.Equal("", l.Scope.ScopeName);
            },
            l => Assert.Equal("2", l.Message),
            l => Assert.Equal("3", l.Message),
            l => Assert.Equal("4", l.Message),
            l => Assert.Equal("5", l.Message),
            l => Assert.Equal("6", l.Message),
            l => Assert.Equal("7", l.Message),
            l => Assert.Equal("8", l.Message),
            l => Assert.Equal("9", l.Message),
            l => Assert.Equal("10", l.Message));
    }

    [Fact]
    public void AddLogs_Error_UnviewedCount()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Trace),
                            CreateLogRecord(time: s_testTime.AddMinutes(2), message: "2", severity: SeverityNumber.Debug),
                            CreateLogRecord(time: s_testTime.AddMinutes(3), message: "3", severity: SeverityNumber.Info),
                            CreateLogRecord(time: s_testTime.AddMinutes(4), message: "4", severity: SeverityNumber.Warn),
                            CreateLogRecord(time: s_testTime.AddMinutes(5), message: "5", severity: SeverityNumber.Error),
                            CreateLogRecord(time: s_testTime.AddMinutes(6), message: "6", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var unviewedCounts1 = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.True(unviewedCounts1.TryGetValue(repository.GetApplication("1")!, out var unviewedCount1));
        Assert.Equal(2, unviewedCount1);

        Assert.True(unviewedCounts1.TryGetValue(repository.GetApplication("2")!, out var unviewedCount2));
        Assert.Equal(1, unviewedCount2);

        repository.MarkViewedErrorLogs("1");

        var unviewedCounts2 = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.False(unviewedCounts2.TryGetValue(repository.GetApplication("1")!, out _));

        Assert.True(unviewedCounts2.TryGetValue(repository.GetApplication("2")!, out unviewedCount2));
        Assert.Equal(1, unviewedCount2);

        repository.MarkViewedErrorLogs(null);

        var unviewedCounts3 = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.False(unviewedCounts3.TryGetValue(repository.GetApplication("1")!, out _));
        Assert.False(unviewedCounts3.TryGetValue(repository.GetApplication("2")!, out _));
    }

    [Fact]
    public void AddLogs_Error_UnviewedCount_WithReadSubscriptionAll()
    {
        // Arrange
        var repository = CreateRepository();
        using var subscription = repository.OnNewLogs(applicationId: null, SubscriptionType.Read, () => Task.CompletedTask);

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Error),
                        }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var unviewedCounts = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.False(unviewedCounts.TryGetValue(repository.GetApplication("1")!, out _));
        Assert.False(unviewedCounts.TryGetValue(repository.GetApplication("2")!, out _));
    }

    [Fact]
    public void AddLogs_Error_UnviewedCount_WithReadSubscriptionOneApp()
    {
        // Arrange
        var repository = CreateRepository();
        using var subscription = repository.OnNewLogs(applicationId: "1", SubscriptionType.Read, () => Task.CompletedTask);

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Error),
                        }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var unviewedCounts = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.False(unviewedCounts.TryGetValue(repository.GetApplication("1")!, out _));
        Assert.True(unviewedCounts.TryGetValue(repository.GetApplication("2")!, out var unviewedCount));
        Assert.Equal(1, unviewedCount);
    }

    [Fact]
    public void AddLogs_Error_UnviewedCount_WithNonReadSubscription()
    {
        // Arrange
        var repository = CreateRepository();
        using var subscription = repository.OnNewLogs(applicationId: null, SubscriptionType.Other, () => Task.CompletedTask);

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Error),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var unviewedCounts = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.True(unviewedCounts.TryGetValue(repository.GetApplication("1")!, out var unviewedCount));
        Assert.Equal(1, unviewedCount);
    }

    [Fact]
    public void GetLogs_UnknownApplication()
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
                        Scope = CreateScope("TestLogger"),
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
        repository.OnNewLogs(applications[0].InstanceId, SubscriptionType.Read, () =>
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
                        Scope = CreateScope("TestLogger"),
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
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);
        Assert.False(onNewApplicationsCalled, "Callback shouldn't have been called because subscription was disposed.");
    }
}
