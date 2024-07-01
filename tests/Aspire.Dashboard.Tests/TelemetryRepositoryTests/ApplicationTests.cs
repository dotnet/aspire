// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Dashboard.Tests.TelemetryRepositoryTests.TestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class ApplicationTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetApplicationByCompositeName()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app2"),
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
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "2", spanId: "2-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "2-1")
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);

        // Act 1
        var applications = repository.GetApplications();

        // Assert 1
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("app1", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            },
            app =>
            {
                Assert.Equal("app2", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        // Act 2
        var app1 = repository.GetApplicationByCompositeName("app1-TestId");
        var app2 = repository.GetApplicationByCompositeName("APP2-TESTID");
        var notFound = repository.GetApplicationByCompositeName("APP2_TESTID");

        // Assert 2
        Assert.NotNull(app1);
        Assert.Equal("app1", app1.ApplicationName);
        Assert.Equal(applications[0], app1);

        Assert.NotNull(app2);
        Assert.Equal("app2", app2.ApplicationName);
        Assert.Equal(applications[1], app2);

        Assert.Null(notFound);
    }
}
