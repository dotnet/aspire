// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AddMongoDBTests
{
    [Fact]
    public void AddMongoDBContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddMongoDB("mongodb");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MongoDBServerResource>());
        Assert.Equal("mongodb", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(27017, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("7.0.5", containerAnnotation.Tag);
        Assert.Equal("mongo", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void AddMongoDBContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMongoDB("mongodb", 9813);

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MongoDBServerResource>());
        Assert.Equal("mongodb", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(27017, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(9813, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("7.0.5", containerAnnotation.Tag);
        Assert.Equal("mongo", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void MongoDBCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddMongoDB("mongodb")
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

    [Fact]
    public void WithMongoExpressAddsContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress();

        Assert.Single(builder.Resources.OfType<MongoExpressContainerResource>());
    }

    [Fact]
    public void WithMongoExpressOnMultipleResources()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress();
        builder.AddMongoDB("mongo2").WithMongoExpress();

        Assert.Equal(2, builder.Resources.OfType<MongoExpressContainerResource>().Count());
    }
}
