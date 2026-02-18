// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.ClickHouse.Driver.Tests;

public class ClickHouseDriverPublicApiTests
{
    [Fact]
    public void AddClickHouseDataSourceShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "clickhouse";

        var action = () => builder.AddClickHouseDataSource(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddClickHouseDataSourceShouldThrowWhenConnectionNameIsNull()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        string connectionName = null!;

        var action = () => builder.AddClickHouseDataSource(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddClickHouseDataSourceShouldThrowWhenConnectionNameIsEmpty()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = string.Empty;

        var action = () => builder.AddClickHouseDataSource(connectionName);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedClickHouseDataSourceShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "clickhouse";

        var action = () => builder.AddKeyedClickHouseDataSource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeyedClickHouseDataSourceShouldThrowWhenNameIsNull()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        string name = null!;

        var action = () => builder.AddKeyedClickHouseDataSource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddKeyedClickHouseDataSourceShouldThrowWhenNameIsEmpty()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = string.Empty;

        var action = () => builder.AddKeyedClickHouseDataSource(name);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
