// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AddMongoDBTests
{
    [Fact]
    public void AddMongoDBContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddMongoDBContainer("mongodb");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MongoDBContainerResource>());
        Assert.Equal("mongodb", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<ServiceBindingAnnotation>());
        Assert.Equal(27017, serviceBinding.ContainerPort);
        Assert.False(serviceBinding.IsExternal);
        Assert.Equal("tcp", serviceBinding.Name);
        Assert.Null(serviceBinding.Port);
        Assert.Equal(ProtocolType.Tcp, serviceBinding.Protocol);
        Assert.Equal("tcp", serviceBinding.Transport);
        Assert.Equal("tcp", serviceBinding.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("mongo", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void AddMongoDBContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMongoDBContainer("mongodb", 9813);

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MongoDBContainerResource>());
        Assert.Equal("mongodb", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var serviceBinding = Assert.Single(containerResource.Annotations.OfType<ServiceBindingAnnotation>());
        Assert.Equal(27017, serviceBinding.ContainerPort);
        Assert.False(serviceBinding.IsExternal);
        Assert.Equal("tcp", serviceBinding.Name);
        Assert.Equal(9813, serviceBinding.Port);
        Assert.Equal(ProtocolType.Tcp, serviceBinding.Protocol);
        Assert.Equal("tcp", serviceBinding.Transport);
        Assert.Equal("tcp", serviceBinding.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("mongo", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void MongoDBCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddMongoDBContainer("mongodb")
            .WithAnnotation(
                new AllocatedEndpointAnnotation("mybinding",
                ProtocolType.Tcp,
                "localhost",
                27017,
                "https"
            ))
            .AddDatabase("mydatabase");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<MongoDBDatabaseResource>());
        var connectionString = connectionStringResource.GetConnectionString();

        Assert.Equal("mongodb://localhost:27017/mydatabase", connectionString);
    }
}
