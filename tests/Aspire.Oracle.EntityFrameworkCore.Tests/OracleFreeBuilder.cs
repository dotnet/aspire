// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Testcontainers.Oracle;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;
public sealed class OracleFreeBuilder : ContainerBuilder<OracleFreeBuilder, OracleContainer, OracleConfiguration>
{
    public const string DefaultUsername = "oracle";
    public const string DefaultPassword = "oracle";

    public OracleFreeBuilder()
        : this(new OracleConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    public OracleFreeBuilder(OracleConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        DockerResourceConfiguration = resourceConfiguration;
    }

    protected override OracleConfiguration DockerResourceConfiguration { get; }

    public OracleFreeBuilder WithUsername(string username)
    {
        return Merge(DockerResourceConfiguration, new OracleConfiguration(username: username))
            .WithEnvironment("APP_USER", username);
    }

    public OracleFreeBuilder WithPassword(string password)
    {
        return Merge(DockerResourceConfiguration, new OracleConfiguration(password: password))
            .WithEnvironment("ORACLE_PASSWORD", password)
            .WithEnvironment("APP_USER_PASSWORD", password);
    }

    public override OracleContainer Build()
    {
        Validate();
        return new OracleContainer(DockerResourceConfiguration);
    }

    protected override OracleFreeBuilder Init()
    {
        return base.Init()
            .WithPortBinding(1521, true)
            .WithDatabase("FREEPDB1")
            .WithUsername(DefaultUsername)
            .WithPassword(DefaultPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("DATABASE IS READY TO USE!"));
    }

    protected override void Validate()
    {
        base.Validate();

        _ = Guard.Argument(DockerResourceConfiguration.Password, nameof(DockerResourceConfiguration.Password))
            .NotNull()
            .NotEmpty();
    }

    protected override OracleFreeBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new OracleConfiguration(resourceConfiguration));
    }

    protected override OracleFreeBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new OracleConfiguration(resourceConfiguration));
    }

    protected override OracleFreeBuilder Merge(OracleConfiguration oldValue, OracleConfiguration newValue)
    {
        return new OracleFreeBuilder(new OracleConfiguration(oldValue, newValue));
    }

    private OracleFreeBuilder WithDatabase(string database)
    {
        return Merge(DockerResourceConfiguration, new OracleConfiguration(database: database));
    }
}
