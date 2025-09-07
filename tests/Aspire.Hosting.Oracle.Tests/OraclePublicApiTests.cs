// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Oracle.Tests;

public class OraclePublicApiTests
{
    [Fact]
    public void AddOracleShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "oracle";

        var action = () => builder.AddOracle(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddOracleShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddOracle(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string name = "db";

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
            .AddOracle("oracle");
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
    public void WithDataBindMountShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddOracle("oracle");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string source = "/opt/oracle/scripts/startup";

#pragma warning disable CS0618 // Type or member is obsolete
        var action = () => builder.WithInitBindMount(source);
#pragma warning restore CS0618 // Type or member is obsolete

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithInitBindMountShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddOracle("oracle");
        var source = isNull ? null! : string.Empty;

#pragma warning disable CS0618 // Type or member is obsolete
        var action = () => builder.WithInitBindMount(source);
#pragma warning restore CS0618 // Type or member is obsolete

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithInitFilesShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string source = "/opt/oracle/scripts/startup";

        var action = () => builder.WithInitFiles(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithInitFilesShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddOracle("oracle");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithInitFiles(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithDbSetupBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<OracleDatabaseServerResource> builder = null!;
        const string source = "/opt/oracle/scripts/setup";

        var action = () => builder.WithDbSetupBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDbSetupBindMountShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddOracle("oracle");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithDbSetupBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorOracleDatabaseResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddOracle("oracle");
        var name = isNull ? null! : string.Empty;
        const string databaseName = "db";
        var parent = builder.Resource;

        var action = () => new OracleDatabaseResource(name, databaseName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorOracleDatabaseResourceShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddOracle("oracle");
        const string name = "oracle";
        var databaseName = isNull ? null! : string.Empty;
        var parent = builder.Resource;

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
        const string databaseName = "db";
        OracleDatabaseServerResource parent = null!;

        var action = () => new OracleDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorOracleDatabaseServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        var password = new ParameterResource("password", (p) => "password");

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
