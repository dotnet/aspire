// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer.Tests;

public class MicrosoftEntityFrameworkCoreSqlServerPublicApiTests
{
    [Fact]
    public void AddSqlServerDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "sqldb";

        var action = () => builder.AddSqlServerDbContext<DbContext>(
            connectionName,
            default(Action<MicrosoftEntityFrameworkCoreSqlServerSettings>?),
            default(Action<DbContextOptionsBuilder>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddSqlServerDbContextShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddSqlServerDbContext<DbContext>(
            connectionName,
            default(Action<MicrosoftEntityFrameworkCoreSqlServerSettings>?),
            default(Action<DbContextOptionsBuilder>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void EnrichSqlServerDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var action = () => builder.EnrichSqlServerDbContext<DbContext>(default(Action<MicrosoftEntityFrameworkCoreSqlServerSettings>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }
}