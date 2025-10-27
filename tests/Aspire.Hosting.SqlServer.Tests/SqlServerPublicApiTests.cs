// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.SqlServer.Tests;

public class SqlServerPublicApiTests
{
    [Fact]
    public void AddSqlServerShouldThrowWhenBuilderIsNull()
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
    public void AddSqlServerShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
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
        var builder = TestDistributedApplicationBuilder.Create()
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
        var builder = TestDistributedApplicationBuilder.Create()
            .AddSqlServer("SqlServer");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string databaseName = "SqlServer";
        const string parameterName = nameof(parameterName);
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, parameterName);
        const string resourceName = nameof(resourceName);
        var parent = new SqlServerServerResource(resourceName, password);

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
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "sql";
        var databaseName = isNull ? null! : string.Empty;
        const string parameterName = nameof(parameterName);
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, parameterName);
        const string resourceName = nameof(resourceName);
        var parent = new SqlServerServerResource(resourceName, password);

        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerDatabaseResourceShouldThrowWhenParentIsNull()
    {
        const string name = "sql";
        const string databaseName = "SqlServer";
        SqlServerServerResource parent = null!;

        var action = () => new SqlServerDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorSqlServerServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string parameterName = nameof(parameterName);
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, parameterName);

        var action = () => new SqlServerServerResource(name, password);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorSqlServerServerResourceShouldThrowWhenPasswordIsNull()
    {
        const string name = "sql";
        ParameterResource password = null!;

        var action = () => new SqlServerServerResource(name, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(password), exception.ParamName);
    }
}
