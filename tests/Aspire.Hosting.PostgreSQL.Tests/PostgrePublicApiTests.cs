// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.PostgreSQL.Tests;

public class PostgresPublicApiTests
{
    [Fact]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;
        const string name = "Postgres";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var postgres = builder.AddPostgres("Postgres");
        string name = null!;

        var action = () => postgres.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddPostgresContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Postgres";

        var action = () => builder.AddPostgres(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddPostgresContainerShouldThrowWhenNameIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        string name = null!;

        var action = () => builder.AddPostgres(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorPgAdminContainerResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;

        var action = () => new PgAdminContainerResource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorPostgresDatabaseResourceShouldThrowWhenDatabaseNameIsNull()
    {
        const string name = "PostgresDatabase";
        string databaseName = null!;
        var builder = TestDistributedApplicationBuilder.Create();
        var parameterResource = builder.AddParameter("password");
        var postgresParentResource = new PostgresServerResource("PostgresServer", default(ParameterResource?), parameterResource.Resource);

        var action = () => new PostgresDatabaseResource(name, databaseName, postgresParentResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(databaseName), exception.ParamName);
    }

    [Fact]
    public void CtorPostgresDatabaseResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        const string databaseName = "db";
        var builder = TestDistributedApplicationBuilder.Create();
        var parameterResource = builder.AddParameter("password");
        var postgresParentResource = new PostgresServerResource("PostgresServer", default(ParameterResource?), parameterResource.Resource);

        var action = () => new PostgresDatabaseResource(name, databaseName, postgresParentResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorPostgresDatabaseResourceShouldThrowWhenPostgresParentResourceIsNull()
    {
        const string name = "PostgresDatabase";
        const string databaseName = "db";
        PostgresServerResource postgresParentResource = null!;

        var action = () => new PostgresDatabaseResource(name, databaseName, postgresParentResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(postgresParentResource), exception.ParamName);
    }

    [Fact]
    public void CtorPostgresServerResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        var builder = TestDistributedApplicationBuilder.Create();
        var password = builder.AddParameter("password");

        var action = () => new PostgresServerResource(name, default(ParameterResource?), password.Resource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorPostgresServerResourceShouldThrowWhenPasswordIsNull()
    {
        const string name = "PostgresServer";
        ParameterResource password = null!;

        var action = () => new PostgresServerResource(name, default(ParameterResource?), password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(password), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;
        const string source = "/var/lib/postgresql/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var postgres = builder.AddPostgres("Postgres");
        string source = null!;

        var action = () => postgres.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithHostPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PgAdminContainerResource> builder = null!;

        var action = () => builder.WithHostPort(default(int?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;
        const string source = "/docker-entrypoint-initdb.d";

        var action = () => builder.WithInitBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithInitBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var postgres = builder.AddPostgres("Postgres");
        string source = null!;

        var action = () => postgres.WithInitBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithPgAdminShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;

        var action = () => builder.WithPgAdmin();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }
}
