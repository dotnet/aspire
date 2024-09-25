// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.SurrealDb.Tests;

public class SurrealDbPublicApiTests
{
    [Fact]
    public void AddSurrealServerContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "surreal";

        var action = () => builder.AddSurrealServer(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddSurrealServerContainerShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        string name = null!;

        var action = () => builder.AddSurrealServer(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SurrealDbNamespaceResource> builder = null!;
        const string name = "surreal";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([])
            .AddSurrealServer("surreal")
            .AddNamespace("ns");
        string name = null!;

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsEmpty()
    {
        var builder = DistributedApplication.CreateBuilder([])
            .AddSurrealServer("surreal")
            .AddNamespace("ns");
        string name = "";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SurrealDbServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SurrealDbServerResource> builder = null!;
        const string source = "/surreal/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var surrealServer = builderResource.AddSurrealServer("surreal");
        string source = null!;

        var action = () => surrealServer.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerServerResourceShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        string name = null!;
        const string key = nameof(key);
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, key, special: false);

        var action = () => new SurrealDbServerResource(name, null, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerDatabaseResourceShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = null!;
        string namespaceName = "ns1";
        string databaseName = "db1";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SurrealDbServerResource("surreal", null, password);
        var nsParent = new SurrealDbNamespaceResource("ns", namespaceName, parent);
        var action = () => new SurrealDbDatabaseResource(name, databaseName, nsParent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerDatabaseResourceShouldThrowWhenNameIsEmpty()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "";
        string namespaceName = "ns1";
        string databaseName = "db1";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SurrealDbServerResource("surreal", null, password);
        var nsParent = new SurrealDbNamespaceResource("ns", namespaceName, parent);
        var action = () => new SurrealDbDatabaseResource(name, databaseName, nsParent);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerNamespaceResourceShouldThrowWhenNamespaceNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string namespaceName = null!;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SurrealDbServerResource("surreal", null, password);
        var action = () => new SurrealDbNamespaceResource("ns", namespaceName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(namespaceName), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerNamespaceResourceShouldThrowWhenNamespaceNameIsEmpty()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string namespaceName = "";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SurrealDbServerResource("surreal", null, password);
        var action = () => new SurrealDbNamespaceResource("ns", namespaceName, parent);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(namespaceName), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerDatabaseResourceShouldThrowWhenDatabaseNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "surreal";
        string namespaceName = "ns";
        string databaseName = null!;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SurrealDbServerResource("surreal", null, password);
        var nsParent = new SurrealDbNamespaceResource("ns", namespaceName, parent);
        var action = () => new SurrealDbDatabaseResource(name, databaseName, nsParent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerDatabaseResourceShouldThrowWhenDatabaseNameIsEmpty()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "surreal";
        string namespaceName = "ns";
        string databaseName = null!;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        var parent = new SurrealDbServerResource("surreal", null, password);
        var nsParent = new SurrealDbNamespaceResource("ns", namespaceName, parent);
        var action = () => new SurrealDbDatabaseResource(name, databaseName, nsParent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorSurrealServerDatabaseResourceShouldThrowWhenParentIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        string name = "surreal";
        string databaseName = "db1";
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);
        SurrealDbNamespaceResource parent = null!;
        var action = () => new SurrealDbDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }
}
