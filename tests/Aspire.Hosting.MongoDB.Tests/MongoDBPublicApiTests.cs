// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.MongoDB.Tests;

public class MongoDBPublicApiTests
{
    [Fact]
    public void AddMongoDBContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "MongoDB";

        var action = () => builder.AddMongoDB(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddMongoDBContainerShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        string name = null!;

        var action = () => builder.AddMongoDB(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MongoDBServerResource> builder = null!;
        const string name = "db1";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsNull()
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var MongoDB = builderResource.AddMongoDB("MongoDB");
        string name = null!;

        var action = () => MongoDB.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MongoDBServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MongoDBServerResource> builder = null!;
        const string source = "/MongoDB/storage";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var MongoDB = builderResource.AddMongoDB("MongoDB");
        string source = null!;

        var action = () => MongoDB.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MongoDBServerResource> builder = null!;

        var action = () => builder.WithInitBindMount("init.js");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithMongoExpressShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MongoDBServerResource> builder = null!;

        var action = () => builder.WithMongoExpress();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenSourceIsNull()
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var MongoDB = builderResource.AddMongoDB("MongoDB");
        string source = null!;

        var action = () => MongoDB.WithInitBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithHostPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MongoExpressContainerResource> builder = null!;

        var action = () => builder.WithHostPort(6601);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void CtorMongoDBServerResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;

        var action = () => new MongoDBServerResource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorMongoMongoDBDatabaseResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        var databaseName = "db1";
        var parent = new MongoDBServerResource("mongodb");

        var action = () => new MongoDBDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorMongoMongoDBDatabaseResourceShouldThrowWhenDatabaseNameIsNull()
    {
        var name = "mongodb";
        string databaseName = null!;
        var parent = new MongoDBServerResource(name);

        var action = () => new MongoDBDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorMongoMongoDBDatabaseResourceShouldThrowWhenDatabaseParentIsNull()
    {
        var name = "mongodb";
        var databaseName = "db1";
        MongoDBServerResource parent = null!;

        var action = () => new MongoDBDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }

    [Fact]
    public void CtorMongoExpressContainerResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;

        var action = () => new MongoExpressContainerResource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
