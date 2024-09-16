// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.MongoDB.Tests;

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
        Assert.Equal(27017, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MongoDBContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MongoDBContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MongoDBContainerImageTags.Registry, containerAnnotation.Registry);
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
        Assert.Equal(27017, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(9813, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MongoDBContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MongoDBContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MongoDBContainerImageTags.Registry, containerAnnotation.Registry);
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
        Assert.Equal("mongodb://{mongodb.bindings.tcp.host}:{mongodb.bindings.tcp.port}/mydatabase", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task MongoDBCreatesConnectionStringWithReplicaSet()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddMongoDB("mongodb")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017))
            .WithReplicaSet("myreplset")
            .AddDatabase("mydatabase");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dbResource = Assert.Single(appModel.Resources.OfType<MongoDBDatabaseResource>());
        var serverResource = dbResource.Parent as IResourceWithConnectionString;
        var connectionStringResource = dbResource as IResourceWithConnectionString;
        Assert.NotNull(connectionStringResource);
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal("mongodb://localhost:27017/?replicaSet=myreplset", await serverResource.GetConnectionStringAsync());
        Assert.Equal("mongodb://{mongodb.bindings.tcp.host}:{mongodb.bindings.tcp.port}/?replicaSet=myreplset", serverResource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("mongodb://localhost:27017/mydatabase?replicaSet=myreplset", connectionString);
        Assert.Equal("mongodb://{mongodb.bindings.tcp.host}:{mongodb.bindings.tcp.port}/mydatabase?replicaSet=myreplset", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void WithMongoExpressAddsContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMongoDB("mongo")
            .WithMongoExpress();

        Assert.Single(builder.Resources.OfType<MongoExpressContainerResource>());
    }

    [Fact]
    public void WithMongoExpressSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customongoexpresscontainer");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.Single(builder.Resources.OfType<MongoExpressContainerResource>());
        var containerAnnotation = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("example.mycompany.com", containerAnnotation.Registry);
        Assert.Equal("customongoexpresscontainer", containerAnnotation.Image);
        Assert.Equal("someothertag", containerAnnotation.Tag);
    }

    [Fact]
    public void WithMongoExpressSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.Single(builder.Resources.OfType<MongoExpressContainerResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Fact]
    public async Task WithMongoExpressUsesContainerHost()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMongoDB("mongo")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000))
            .WithMongoExpress();

        var mongoExpress = Assert.Single(builder.Resources.OfType<MongoExpressContainerResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(mongoExpress, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Collection(env,
            e =>
            {
                Assert.Equal("ME_CONFIG_MONGODB_URL", e.Key);
                Assert.Equal($"mongodb://mongo:27017/?directConnection=true", e.Value);
            },
            e =>
            {
                Assert.Equal("ME_CONFIG_BASICAUTH", e.Key);
                Assert.Equal("false", e.Value);
            });
    }

    [Fact]
    public async Task WithMongoExpressWithReplicaSet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMongoDB("mongo")
            .WithReplicaSet("myreplicaset")
            .WithMongoExpress();

        var mongoExpress = Assert.Single(builder.Resources.OfType<MongoExpressContainerResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(mongoExpress, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Collection(env,
            e =>
            {
                Assert.Equal("ME_CONFIG_MONGODB_URL", e.Key);
                Assert.Equal($"mongodb://mongo:27017/?directConnection=true&replicaSet=myreplicaset", e.Value);
            },
            e =>
            {
                Assert.Equal("ME_CONFIG_BASICAUTH", e.Key);
                Assert.Equal("false", e.Value);
            });
    }

    [Fact]
    public void WithMongoExpressOnMultipleResources()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
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

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "mongodb://{mongo.bindings.tcp.host}:{mongo.bindings.tcp.port}",
              "image": "{{MongoDBContainerImageTags.Registry}}/{{MongoDBContainerImageTags.Image}}:{{MongoDBContainerImageTags.Tag}}",
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 27017
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, mongoManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "mongodb://{mongo.bindings.tcp.host}:{mongo.bindings.tcp.port}/mydb"
            }
            """;
        Assert.Equal(expectedManifest, dbManifest.ToString());
    }

    [InlineData(null)]
    [InlineData(10002)]
    [Theory]
    public async Task VerifyManifestWithReplicaSet(int? customPort)
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var mongo = appBuilder.AddMongoDB("mongo", customPort)
            .WithReplicaSet();
        var db = mongo.AddDatabase("mydb");

        var mongoManifest = await ManifestUtils.GetManifest(mongo.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "mongodb://{mongo.bindings.tcp.host}:{mongo.bindings.tcp.port}/?replicaSet=rs0",
              "image": "{{MongoDBContainerImageTags.Registry}}/{{MongoDBContainerImageTags.Image}}:{{MongoDBContainerImageTags.Tag}}",
              "args": [
                "--replSet",
                "rs0",
                "--bind_ip_all",
                "--port",
                "{{customPort ?? 27017}}"
              ],
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": {{customPort ?? 27017}}
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, mongoManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "mongodb://{mongo.bindings.tcp.host}:{mongo.bindings.tcp.port}/mydb?replicaSet=rs0"
            }
            """;
        Assert.Equal(expectedManifest, dbManifest.ToString());
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db = builder.AddMongoDB("mongo1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddMongoDB("mongo1")
            .AddDatabase("db");

        var db = builder.AddMongoDB("mongo2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var mongo1 = builder.AddMongoDB("mongo1");

        var db1 = mongo1.AddDatabase("db1", "customers1");
        var db2 = mongo1.AddDatabase("db2", "customers2");

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("mongodb://{mongo1.bindings.tcp.host}:{mongo1.bindings.tcp.port}/customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("mongodb://{mongo1.bindings.tcp.host}:{mongo1.bindings.tcp.port}/customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddMongoDB("mongo1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddMongoDB("mongo2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("mongodb://{mongo1.bindings.tcp.host}:{mongo1.bindings.tcp.port}/imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("mongodb://{mongo2.bindings.tcp.host}:{mongo2.bindings.tcp.port}/imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
