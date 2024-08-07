// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Milvus.Client.Tests;

public class MilvusClientPublicApiTests
{
    [Fact]
    public void AddMilvusClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var connectionName = "milvus";

        var action = () => builder.AddMilvusClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddMilvusClientShouldThrowWhenNameIsNull()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        string connectionName = null!;

        var action = () => builder.AddMilvusClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddMilvusClientShouldThrowWhenNameIsEmpty()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        string connectionName = "";

        var action = () => builder.AddMilvusClient(connectionName);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedMilvusClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var connectionName = "milvus";

        var action = () => builder.AddKeyedMilvusClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeyedMilvusClientShouldThrowWhenNameIsNull()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        string name = null!;

        var action = () => builder.AddKeyedMilvusClient(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddKeyedMilvusClientShouldThrowWhenNameIsEmpty()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        string name = "";

        var action = () => builder.AddKeyedMilvusClient(name);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
