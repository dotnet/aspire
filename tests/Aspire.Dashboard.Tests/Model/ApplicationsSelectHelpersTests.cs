// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ApplicationsSelectHelpersTests
{
    [Fact]
    public void GetApplication_SameNameAsReplica_GetInstance()
    {
        // Arrange
        var apps = new Dictionary<string, OtlpApplication>();

        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplication(apps, name: "app", instanceId: "app"),
            CreateOtlpApplication(apps, name: "app", instanceId: "app-abc"),
            CreateOtlpApplication(apps, name: "singleton", instanceId: "singleton-abc")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("app", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaSet, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app (app)", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
                Assert.Equal("app", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app (app-abc)", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
                Assert.Equal("app-abc", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("singleton", app.Name);
                Assert.Equal(OtlpApplicationType.Singleton, app.Id!.Type);
                Assert.Equal("singleton-abc", app.Id!.InstanceId);
            });

        // Act
        var app = appVMs.GetApplication(NullLogger.Instance, "app (app-abc)", null!);

        // Assert
        Assert.Equal("app-abc", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
    }

    [Fact]
    public void GetApplication_NameDifferentByCase_Merge()
    {
        // Arrange
        var apps = new Dictionary<string, OtlpApplication>();

        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplication(apps, name: "app", instanceId: "app"),
            CreateOtlpApplication(apps, name: "APP", instanceId: "app-abc")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("app", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaSet, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("APP (app-abc)", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
                Assert.Equal("app-abc", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("APP (app)", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
                Assert.Equal("app", app.Id!.InstanceId);
            });

        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider(testSink)));

        // Act
        var app = appVMs.GetApplication(factory.CreateLogger("Test"), "app (app)", null!);

        // Assert
        Assert.Equal("app", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
        Assert.Empty(testSink.Writes);
    }

    [Fact]
    public void GetApplication_MultipleMatches_UseFirst()
    {
        // Arrange
        var apps = new Dictionary<string, OtlpApplication>();

        var appVMs = new List<SelectViewModel<ResourceTypeDetails>>
        {
            new SelectViewModel<ResourceTypeDetails>() { Name = "test", Id = ResourceTypeDetails.CreateSingleton("test-abc") },
            new SelectViewModel<ResourceTypeDetails>() { Name = "test", Id = ResourceTypeDetails.CreateSingleton("test-def") }
        };

        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider(testSink)));

        // Act
        var app = appVMs.GetApplication(factory.CreateLogger("Test"), "test", null!);

        // Assert
        Assert.Equal("test-abc", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.Singleton, app.Id!.Type);
        Assert.Single(testSink.Writes);
    }

    private static OtlpApplication CreateOtlpApplication(Dictionary<string, OtlpApplication> apps, string name, string instanceId)
    {
        return new OtlpApplication(new Resource
        {
            Attributes =
                {
                    new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = name } },
                    new KeyValue { Key = "service.instance.id", Value = new AnyValue { StringValue = instanceId } }
                }
        }, apps, NullLogger.Instance, new TelemetryLimitOptions());
    }
}
