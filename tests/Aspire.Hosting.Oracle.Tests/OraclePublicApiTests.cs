// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Oracle.Tests;

public class OraclePublicApiTests
{
    [Fact]
    public void AddOracleShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "oracle";

        var action = () => builder.AddOracle(
            name,
            default(IResourceBuilder<ParameterResource>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddOracleShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddOracle(
            name,
            default(IResourceBuilder<ParameterResource>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string name = "oracle";

        var action = () => builder.AddDatabase(name, default(string?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDatabaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var oracle = builder.AddOracle("oracle");
        var name = isNull ? null! : string.Empty;

        var action = () => oracle.AddDatabase(name, default(string?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string source = "/opt/oracle/oradata";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var oracle = builder.AddOracle("oracle");
        var source = isNull ? null! : string.Empty;

        var action = () => oracle.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string source = "/opt/oracle/oradata";

        var action = () => builder.WithInitBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithInitBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var oracle = builder.AddOracle("oracle");
        var source = isNull ? null! : string.Empty;

        var action = () => oracle.WithInitBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithDbSetupBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string source = "/opt/oracle/oradata";

        var action = () => builder.WithDbSetupBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDbSetupBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var oracle = builder.AddOracle("oracle");
        var source = isNull ? null! : string.Empty;

        var action = () => oracle.WithDbSetupBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorOracleDatabaseResourceShouldThrowWhenNameIsNull(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var adminPassword = builder.AddParameter("Password");

        var name = isNull ? null! : string.Empty;
        const string databaseName = "oracledb";
        var parent = new OracleDatabaseServerResource("oracledb", adminPassword.Resource);

        var action = () => new OracleDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorOracleDatabaseResourceShouldThrowWhenDatabaseNameIsNull(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var adminPassword = builder.AddParameter("Password");

        const string name = "oracle";
        var databaseName = isNull ? null! : string.Empty;
        var parent = new OracleDatabaseServerResource("oracledb", adminPassword.Resource);

        var action = () => new OracleDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorOracleDatabaseResourceShouldThrowWhenParentIsNull()
    {
        const string name = "oracle";
        const string databaseName = "oracledb";
        OracleDatabaseServerResource parent = null!;

        var action = () => new OracleDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtoOracleDatabaseServerResourceShouldThrowWhenNameIsNull(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var adminPassword = builder.AddParameter("Password");

        var name = isNull ? null! : string.Empty;
        var password = adminPassword.Resource;

        var action = () => new OracleDatabaseServerResource(name, password);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorOracleDatabaseServerResourceShouldThrowWhenPasswordIsNull()
    {
        const string name = "oracle";
        ParameterResource password = null!;

        var action = () => new OracleDatabaseServerResource(name, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(password), exception.ParamName);
    }
}
