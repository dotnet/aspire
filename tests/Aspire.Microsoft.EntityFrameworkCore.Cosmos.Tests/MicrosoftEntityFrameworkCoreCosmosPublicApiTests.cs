// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

public class MicrosoftEntityFrameworkCoreCosmosPublicApiTests
{
    [Fact]
    public void AddCosmosDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "cosmos";
        const string databaseName = "cosmosdb";

        var action = () => builder.AddCosmosDbContext<DbContext>(
            connectionName,
            databaseName,
            default(Action<EntityFrameworkCoreCosmosSettings>?),
            default(Action<DbContextOptionsBuilder>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddCosmosDbContextShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;
        const string databaseName = "cosmosdb";

        var action = () => builder.AddCosmosDbContext<DbContext>(
            connectionName,
            databaseName,
            default(Action<EntityFrameworkCoreCosmosSettings>?),
            default(Action<DbContextOptionsBuilder>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddCosmosDbContextShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        const string connectionName = "cosmos";
        var databaseName = isNull ? null! : string.Empty;

        var action = () => builder.AddCosmosDbContext<DbContext>(
            connectionName,
            databaseName,
            default(Action<EntityFrameworkCoreCosmosSettings>?),
            default(Action<DbContextOptionsBuilder>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void EnrichCosmosDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var action = () => builder.EnrichCosmosDbContext<DbContext>(default(Action<EntityFrameworkCoreCosmosSettings>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }
}
