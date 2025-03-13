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
        const string connectionName = "mongodb";

        var action = () => builder.AddMongoDBClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddMongoDBClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddMongoDBClient(connectionName);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedMongoDBClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "mongodb";

        var action = () => builder.AddKeyedMongoDBClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedMongoDBClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedMongoDBClient(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
