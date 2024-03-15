// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Utils;
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

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MongoDBServerResource>());
        Assert.Equal("mongodb", containerResource.Name);

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

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MongoDBServerResource>());
        Assert.Equal("mongodb", containerResource.Name);

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
    public async Task MongoDBCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddMongoDB("mongodb")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017))
            .AddDatabase("mydatabase");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dbResource = Assert.Single(appModel.Resources.OfType<MongoDBDatabaseResource>());
        var serverResource = dbResource.Parent as IResourceWithConnectionString;
        var connectionStringResource = dbResource as IResourceWithConnectionString;
        Assert.NotNull(connectionStringResource);
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal("mongodb://localhost:27017", await serverResource.GetConnectionStringAsync());
        Assert.Equal("mongodb://{mongodb.bindings.tcp.host}:{mongodb.bindings.tcp.port}", serverResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("mongodb://localhost:27017/mydatabase", connectionString);
        Assert.Equal("{mongodb.connectionString}/mydatabase", connectionStringResource.ConnectionStringExpression.ValueExpression);
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

    [Fact]
    public async Task VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var mongo = appBuilder.AddMongoDB("mongo");
        var db = mongo.AddDatabase("mydb");

        var mongoManifest = await ManifestUtils.GetManifest(mongo.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = """
            {
              "type": "container.v0",
              "connectionString": "mongodb://{mongo.bindings.tcp.host}:{mongo.bindings.tcp.port}",
              "image": "mongo:7.0.5",
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "containerPort": 27017
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, mongoManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{mongo.connectionString}/mydb"
            }
            """;
        Assert.Equal(expectedManifest, dbManifest.ToString());
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db = builder.AddMongoDB("mongo1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddMongoDB("mongo1")
            .AddDatabase("db");

        var db = builder.AddMongoDB("mongo2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mongo1 = builder.AddMongoDB("mongo1");

        var db1 = mongo1.AddDatabase("db1", "customers1");
        var db2 = mongo1.AddDatabase("db2", "customers2");

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("{mongo1.connectionString}/customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{mongo1.connectionString}/customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db1 = builder.AddMongoDB("mongo1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddMongoDB("mongo2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("{mongo1.connectionString}/imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{mongo2.connectionString}/imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
