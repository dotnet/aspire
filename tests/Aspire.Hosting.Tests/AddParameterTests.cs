// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AddParameterTests
{
    [Fact]
    public void ParametersAreHiddenByDefault()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:pass"] = "pass1";

        appBuilder.AddParameter("pass", secret: true);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>());
        var annotation = parameterResource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.Equal("Hidden", state.State);
        Assert.Collection(state.Properties,
            prop =>
            {
                Assert.Equal("parameter.secret", prop.Name);
                Assert.Equal("True", prop.Value);
            },
            prop =>
            {
                Assert.Equal(CustomResourceKnownProperties.Source, prop.Name);
                Assert.Equal("Parameters:pass", prop.Value);
            },
            prop =>
            {
                Assert.Equal("Value", prop.Name);
                Assert.Equal("pass1", prop.Value);
            });
    }

    [Fact]
    public void MissingParametersAreFailedToStart()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddParameter("pass");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>());
        var annotation = parameterResource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.Equal("FailedToStart", state.State);
        Assert.Collection(state.Properties,
            prop =>
            {
                Assert.Equal("parameter.secret", prop.Name);
                Assert.Equal("False", prop.Value);
            },
            prop =>
            {
                Assert.Equal(CustomResourceKnownProperties.Source, prop.Name);
                Assert.Equal("Parameters:pass", prop.Value);
            },
            prop =>
            {
                Assert.Equal("Value", prop.Name);
                Assert.Contains("configuration key 'Parameters:pass' is missing", prop.Value?.ToString());
            });
    }
}
