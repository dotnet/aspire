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
            CreateOtlpApplication(name: "multiple", instanceId: "instance"),
            CreateOtlpApplication(name: "multiple", instanceId: "instanceabc"),
            CreateOtlpApplication(name: "singleton", instanceId: "instanceabc")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("multiple", app.Name);
                Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("multiple-instance", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("multiple-instance", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("multiple-instanceabc", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("multiple-instanceabc", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("singleton", app.Name);
                Assert.Equal(OtlpApplicationType.Singleton, app.Id!.Type);
                Assert.Equal("singleton-instanceabc", app.Id!.InstanceId);
            });

        // Act
        var app = appVMs.GetApplication(NullLogger.Instance, "multiple-instanceabc", canSelectGrouping: false, null!);

        // Assert
        Assert.Equal("multiple-instanceabc", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
    }

    [Fact]
    public void GetApplication_NameDifferentByCase_Merge()
    {
        // Arrange
        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplication(name: "name", instanceId: "instance"),
            CreateOtlpApplication(name: "NAME", instanceId: "instanceabc")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("name", app.Name);
                Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("NAME-instance", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("name-instance", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("NAME-instanceabc", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("name-instanceabc", app.Id!.InstanceId);
            });

        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider(testSink)));

        // Act
        var app = appVMs.GetApplication(factory.CreateLogger("Test"), "name-instance", canSelectGrouping: false, null!);

        // Assert
        Assert.Equal("name-instance", app.Id!.InstanceId);
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
                Assert.Equal("app-123", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-456", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("app-456", app.Id!.InstanceId);
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
                Assert.Equal("app-123", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("app-456", app.Name);
                Assert.Equal(OtlpApplicationType.Instance, app.Id!.Type);
                Assert.Equal("app-456", app.Id!.InstanceId);
            });

        // Act
        var app = appVMs.GetApplication(NullLogger.Instance, "app", canSelectGrouping: true, null!);

        // Assert
        Assert.Equal("app", app.Name);
        Assert.Equal(OtlpApplicationType.ResourceGrouping, app.Id!.Type);
    }

    [Fact]
    public void GetApplication_CustomServiceName_ReproducesBug()
    {
        // This test reproduces the issue in #9632:
        // When users configure OpenTelemetry with custom service names using
        // .ConfigureResource(b => b.AddService(builder.Environment.ApplicationName)),
        // the dashboard filtering fails to match resources correctly.
        
        // Arrange - Create an application with custom service name
        var customServiceName = "MyCustomService";
        var instanceId = "instance1";
        var originalAppName = "myapp"; // This would be the original app name from app host
        
        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplicationWithCustomServiceName(customServiceName, instanceId)
        });

        Assert.Single(appVMs);
        Assert.Equal(customServiceName, appVMs[0].Name);
        Assert.Equal(OtlpApplicationType.Singleton, appVMs[0].Id!.Type);
        Assert.Equal($"{customServiceName}-{instanceId}", appVMs[0].Id!.InstanceId);

        // Act - Try to get application using original app name (this reproduces the bug scenario)
        var fallback = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = "All" };
        var appByOriginalName = appVMs.GetApplication(NullLogger.Instance, originalAppName, canSelectGrouping: false, fallback);

        // Assert - With the fix, when there's only one application and no exact match is found,
        // it should return that single application instead of falling back to "All"
        Assert.NotEqual(fallback, appByOriginalName);
        Assert.Equal(customServiceName, appByOriginalName.Name);
    }

    [Fact]
    public void GetApplication_CustomServiceName_MultipleApps_ShouldReturnFallback()
    {
        // This test ensures that when there are multiple applications and no exact match,
        // we still return the fallback to avoid false positives
        
        // Arrange - Create multiple applications with custom service names
        var appVMs = ApplicationsSelectHelpers.CreateApplications(new List<OtlpApplication>
        {
            CreateOtlpApplicationWithCustomServiceName("ServiceA", "instance1"),
            CreateOtlpApplicationWithCustomServiceName("ServiceB", "instance2")
        });

        Assert.Equal(2, appVMs.Count);

        // Act - Try to get application using an unmatched name
        var fallback = new SelectViewModel<ResourceTypeDetails> { Id = null, Name = "All" };
        var appByUnmatchedName = appVMs.GetApplication(NullLogger.Instance, "UnmatchedService", canSelectGrouping: false, fallback);

        // Assert - Should return fallback when multiple apps exist and no match is found
        Assert.Equal(fallback, appByUnmatchedName);
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
    
    private static OtlpApplication CreateOtlpApplicationWithCustomServiceName(string customServiceName, string instanceId)
    {
        var resource = new Resource
        {
            Attributes =
                {
                    new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = customServiceName } },
                    new KeyValue { Key = "service.instance.id", Value = new AnyValue { StringValue = instanceId } }
                }
        };
        var applicationKey = OtlpHelpers.GetApplicationKey(resource);

        return new OtlpApplication(applicationKey.Name, applicationKey.InstanceId!, uninstrumentedPeer: false, TelemetryTestHelpers.CreateContext());
    }
}
