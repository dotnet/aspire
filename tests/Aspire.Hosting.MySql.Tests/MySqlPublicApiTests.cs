// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.MySql.Tests;

public class MySqlPublicApiTests
{
    [Fact]
    public void AddMySqlContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        string name = "MySql";

        var action = () => builder.AddMySql(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddMySqlContainerShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        string name = null!;

        var action = () => builder.AddMySql(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MySqlServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MySqlServerResource> builder = null!;
        string source = "/MySql/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var mySql = builder.AddMySql("MySql");
        string source = null!;

        var action = () => mySql.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MySqlServerResource> builder = null!;
        string source = "/MySql/init.sql";

        var action = () => builder.WithInitBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var mySql = builder.AddMySql("MySql");
        string source = null!;

        var action = () => mySql.WithInitBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MySqlServerResource> builder = null!;
        var name = "db";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var mySql = builder.AddMySql("MySql");
        string name = null!;

        var action = () => mySql.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithPhpMyAdminShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MySqlServerResource> builder = null!;

        var action = () => builder.WithPhpMyAdmin();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }
    [Fact]
    public void WithHostPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PhpMyAdminContainerResource> builder = null!;

        var action = () => builder.WithHostPort(6033);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void CtorMySqlServerResourceShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        string name = null!;

        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);

        var action = () => new MySqlServerResource(name, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorMySqlServerResourceShouldThrowWhenPasswordIsNull()
    {
        const string name = "MySql";
        ParameterResource password = null!;

        var action = () => new MySqlServerResource(name, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(password), exception.ParamName);
    }
}
