// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.Extensions.Logging.Abstractions;
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
            CreateOtlpApplication(apps, name: "nodeapp", instanceId: "nodeapp"),
            CreateOtlpApplication(apps, name: "nodeapp", instanceId: "nodeapp-abc"),
            CreateOtlpApplication(apps, name: "singleton", instanceId: "singleton-abc")
        });

        Assert.Collection(appVMs,
            app =>
            {
                Assert.Equal("nodeapp", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaSet, app.Id!.Type);
                Assert.Null(app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("nodeapp", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
                Assert.Equal("nodeapp", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("nodeapp-abc", app.Name);
                Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
                Assert.Equal("nodeapp-abc", app.Id!.InstanceId);
            },
            app =>
            {
                Assert.Equal("singleton", app.Name);
                Assert.Equal(OtlpApplicationType.Singleton, app.Id!.Type);
                Assert.Equal("singleton-abc", app.Id!.InstanceId);
            });

        // Act
        var app = appVMs.GetApplication("nodeapp", null!);

        // Assert
        Assert.Equal("nodeapp", app.Id!.InstanceId);
        Assert.Equal(OtlpApplicationType.ReplicaInstance, app.Id!.Type);
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
