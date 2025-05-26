// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Containers.Tests;

public class DockerSocketBindMountTests
{
    [Fact]
    public void WithDockerSocketBindMountCreatesCorrectAnnotation()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddContainer("container", "none")
            .WithDockerSocketBindMount();

        using var app = appBuilder.Build();

        var containerResource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().GetContainerResources());

        Assert.True(containerResource.TryGetLastAnnotation<ContainerMountAnnotation>(out var mountAnnotation));

        Assert.Equal("/var/run/docker.sock", mountAnnotation.Source);
        Assert.Equal("/var/run/docker.sock", mountAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, mountAnnotation.Type);
        Assert.False(mountAnnotation.IsReadOnly);
    }
}