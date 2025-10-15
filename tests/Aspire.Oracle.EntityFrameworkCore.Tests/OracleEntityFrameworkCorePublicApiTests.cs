// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public class OracleEntityFrameworkCorePublicApiTests
{
    [Fact]
    public void AddOracleDatabaseDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "oracledb";

        var action = () => builder.AddOracleDatabaseDbContext<DbContext>(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddOracleDatabaseDbContextShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddOracleDatabaseDbContext<DbContext>(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void EnrichOracleDatabaseDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var action = () => builder.EnrichOracleDatabaseDbContext<DbContext>();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }
}
