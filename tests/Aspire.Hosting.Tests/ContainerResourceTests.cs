// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ContainerResourceTests
{
    [Fact]
    public void AddContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddContainer("container", "none");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("container", containerResource.Name);
        var containerAnnotation = Assert.IsType<ContainerImageAnnotation>(Assert.Single(containerResource.Annotations));
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("none", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void AddContainerAddsAnnotationMetadataWithTag()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddContainer("container", "none", "nightly");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("container", containerResource.Name);
        var containerAnnotation = Assert.IsType<ContainerImageAnnotation>(Assert.Single(containerResource.Annotations));
        Assert.Equal("nightly", containerAnnotation.Tag);
        Assert.Equal("none", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }
}
