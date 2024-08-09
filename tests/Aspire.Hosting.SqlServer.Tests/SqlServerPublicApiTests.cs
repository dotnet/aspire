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

    [Fact]
    public void AddSqlServerContainerShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        string name = null!;

        var action = () => builder.AddSqlServer(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
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

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([])
            .AddSqlServer("sqlserver");
        string name = null!;

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsEmpty()
    {
        var builder = DistributedApplication.CreateBuilder([])
            .AddSqlServer("sqlserver");
        string name = "";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentException>(action);
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

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var SqlServer = builderResource.AddSqlServer("SqlServer");
        string source = null!;

        var action = () => SqlServer.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerServerResourceShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        string name = null!;
        const string key = nameof(key);
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, key, special: false);

        var action = () => new SqlServerServerResource(name, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = null!;
        var databaseName = "db1";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SqlServerServerResource("sqlserver",password);
        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenNameIsEmpty()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "";
        var databaseName = "db1";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SqlServerServerResource("sqlserver", password);
        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenDatabaseNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "sqlserver";
        string databaseName = null!;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SqlServerServerResource("sqlserver", password);
        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenDatabaseNameIsEmpty()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "sqlserver";
        string databaseName = null!;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SqlServerServerResource("sqlserver", password);
        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenParentIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "sqlserver";
        string databaseName = "db1";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        SqlServerServerResource parent = null!;
        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }
}
