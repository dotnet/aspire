// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.MongoDB.Driver.Tests;

public class MongoDBClientPublicApiTests
{
    [Fact]
    public void AddMongoDBClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        var connectionName = "mongodb";

        var action = () => builder.AddMongoDBClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddMongoDBClientShouldThrowWhenNameIsNull()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        string connectionName = null!;

        var action = () => builder.AddMongoDBClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddMongoDBClientShouldThrowWhenNameIsEmpty()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        string connectionName = "";

        var action = () => builder.AddMongoDBClient(connectionName);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedMongoDBClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        var connectionName = "mongodb";

        var action = () => builder.AddKeyedMongoDBClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeyedMongoDBClientShouldThrowWhenNameIsNull()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        string name = null!;

        var action = () => builder.AddKeyedMongoDBClient(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddKeyedMongoDBClientShouldThrowWhenNameIsEmpty()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        string name = "";

        var action = () => builder.AddKeyedMongoDBClient(name);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
