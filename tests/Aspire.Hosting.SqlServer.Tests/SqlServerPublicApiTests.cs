// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.SqlServer.Tests;

public class SqlServerPublicApiTests
{
    [Fact]
    public void AddSqlServerContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "SqlServer";

        var action = () => builder.AddSqlServer(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddSqlServerContainerShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddSqlServer(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SqlServerServerResource> builder = null!;
        const string name = "SqlServer";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDatabaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([])
            .AddSqlServer("sqlserver");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddDatabase(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SqlServerServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SqlServerServerResource> builder = null!;
        const string source = "/sqlserver/data";

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
        var sqlServer = builderResource.AddSqlServer("SqlServer");
        var source = isNull ? null! : string.Empty;

        var action = () => sqlServer.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorSqlServerServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var name = isNull ? null! : string.Empty;
        const string key = nameof(key);
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, key, special: false);

        var action = () => new SqlServerServerResource(name, password);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var name = isNull ? null! : string.Empty;
        var databaseName = "db1";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SqlServerServerResource("sqlserver", password);

        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var name = "sqlserver";
        var databaseName = isNull ? null! : string.Empty;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SqlServerServerResource("sqlserver", password);

        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenParentIsNull()
    {
        var name = "sqlserver";
        var databaseName = "db1";
        SqlServerServerResource parent = null!;

        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }
}
