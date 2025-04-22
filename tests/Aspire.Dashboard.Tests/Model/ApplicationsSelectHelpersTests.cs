// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
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
        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplication(name: "app", instanceId: "app"),
            CreateOtlpApplication(name: "app", instanceId: "app-abc"),
            CreateOtlpApplication(name: "singleton", instanceId: "singleton-abc")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("app", app.Name);
                Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-app", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("app", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-app-abc", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("app-abc", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("singleton", app.Name);
                Assert.Equal(OtlpApplicationType.Singleton, app.Id!.Type);
                Assert.Equal("singleton-abc", app.Id!.InstanceId);
            });

        // Act
        var app = appVMs.GetApplication(NullLogger.Instance, "app-app-abc", canSelectGrouping: false, null!);

        // Assert
        Assert.Equal("app-abc", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
    }

    [Fact]
    public void GetApplication_NameDifferentByCase_Merge()
    {
        // Arrange
        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplication(name: "app", instanceId: "app"),
            CreateOtlpApplication(name: "APP", instanceId: "app-abc")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("app", app.Name);
                Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("APP-app", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("app", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("APP-app-abc", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("app-abc", app.Id!.InstanceId);
            });

        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider(testSink)));

        // Act
        var app = appVMs.GetApplication(factory.CreateLogger("Test"), "app-app", canSelectGrouping: false, null!);

        // Assert
        Assert.Equal("app", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
        Assert.Empty(testSink.Writes);
    }

    [Fact]
    public void GetApplication_MultipleMatches_UseFirst()
    {
        // Arrange
        var apps = new Dictionary<string, OtlpApplication>();

        var appVMs = new List<SelectViewModel<ResourceTypeDetails>>
        {
            new SelectViewModel<ResourceTypeDetails>() { Name = "test", Id = ResourceTypeDetails.CreateSingleton("test-abc", "test") },
            new SelectViewModel<ResourceTypeDetails>() { Name = "test", Id = ResourceTypeDetails.CreateSingleton("test-def", "test") }
        };

        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider(testSink)));

        // Act
        var app = appVMs.GetApplication(factory.CreateLogger("Test"), "test", canSelectGrouping: false, null!);

        // Assert
        Assert.Equal("test-abc", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.Singleton, app.Id!.Type);
        Assert.Single(testSink.Writes);
    }

    [Fact]
    public void GetApplication_SelectGroup_NotEnabled_ReturnNull()
    {
        // Arrange
        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplication(name: "app", instanceId: "123"),
            CreateOtlpApplication(name: "app", instanceId: "456")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("app", app.Name);
                Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-123", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("123", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-456", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("456", app.Id!.InstanceId);
            });

        // Act
        var app = appVMs.GetApplication(NullLogger.Instance, "app", canSelectGrouping: false, null!);

        // Assert
        Assert.Null(app);
    }

    [Fact]
    public void GetApplication_SelectGroup_Enabled_ReturnGroup()
    {
        // Arrange
        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplication(name: "app", instanceId: "123"),
            CreateOtlpApplication(name: "app", instanceId: "456")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("app", app.Name);
                Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-123", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("123", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-456", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("456", app.Id!.InstanceId);
            });

        // Act
        var app = appVMs.GetApplication(NullLogger.Instance, "app", canSelectGrouping: true, null!);

        // Assert
        Assert.Equal("app", app.Name);
        Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
    }

    private static OtlpApplication CreateOtlpApplication(string name, string instanceId)
    {
        var resource = new Resource
        {
            Attributes =
                {
                    new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = name } },
                    new KeyValue { Key = "service.instance.id", Value = new AnyValue { StringValue = instanceId } }
                }
        };
        var applicationKey = OtlpHelpers.GetApplicationKey(resource);

        return new OtlpApplication(applicationKey.Name, applicationKey.InstanceId!, uninstrumentedPeer: false, TelemetryTestHelpers.CreateContext());
    }
}
