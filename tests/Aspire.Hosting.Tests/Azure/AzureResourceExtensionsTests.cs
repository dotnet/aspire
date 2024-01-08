// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureResourceExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(8081)]
    [InlineData(9007)]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedPort(int? port = null)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.UseEmulator(port);

        var endpointAnnotation = cosmos.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
        Assert.NotNull(endpointAnnotation);

        var actualPort = endpointAnnotation.Port;
        Assert.Equal(port, actualPort);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2.3.97-preview")]
    [InlineData("1.0.7")]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedImageTag(string? imageTag = null)
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.UseEmulator(imageTag: imageTag);

        var containerImageAnnotation = cosmos.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.NotNull(containerImageAnnotation);

        var actualTag = containerImageAnnotation.Tag;
        Assert.Equal(imageTag ?? "latest", actualTag);
    }
}
