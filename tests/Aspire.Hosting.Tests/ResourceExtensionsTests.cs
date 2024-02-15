// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;
public class ResourceExtensionsTests
{
    [Fact]
    public void TryGetContainerImageNameReturnsCorrectFormatWhenShaSupplied()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("grafana", "grafana/grafana", "latest").WithImageSHA256("1adbcc2df3866ff5ec1d836e9d2220c904c7f98901b918d3cc5e1118ab1af991");

        Assert.True(container.Resource.TryGetContainerImageName(out var imageName));
        Assert.Equal("grafana/grafana@sha256:1adbcc2df3866ff5ec1d836e9d2220c904c7f98901b918d3cc5e1118ab1af991", imageName);
    }

    [Fact]
    public void TryGetContainerImageNameReturnsCorrectFormatWhenShaNotSupplied()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("grafana", "grafana/grafana", "10.3.1");

        Assert.True(container.Resource.TryGetContainerImageName(out var imageName));
        Assert.Equal("grafana/grafana:10.3.1", imageName);
    }
}
