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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddMongoDBContainerShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddMongoDB(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDatabaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var mongoDB = builderResource.AddMongoDB("MongoDB");
        var name = isNull ? null! : string.Empty;

        var action = () => mongoDB.AddDatabase(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var mongoDB = builderResource.AddMongoDB("MongoDB");
        var source = isNull ? null! : string.Empty;

        var action = () => mongoDB.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithInitBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var mongoDB = builderResource.AddMongoDB("MongoDB");
        var source = isNull ? null! : string.Empty;

        var action = () => mongoDB.WithInitBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorMongoDBServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;

        var action = () => new MongoDBServerResource(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorMongoMongoDBDatabaseResourceShouldThrowWhenNameIsNull(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        var databaseName = "db1";
        var parent = new MongoDBServerResource("mongodb");

        var action = () => new MongoDBDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorMongoMongoDBDatabaseResourceShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        var name = "mongodb";
        var databaseName = isNull ? null! : string.Empty;
        var parent = new MongoDBServerResource(name);

        var action = () => new MongoDBDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorMongoExpressContainerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;

        var action = () => new MongoExpressContainerResource(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
