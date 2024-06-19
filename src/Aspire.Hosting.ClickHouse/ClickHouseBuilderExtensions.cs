// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ClickHouse;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding ClickHouse resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class ClickHouseBuilderExtensions
{
    private const string UserEnvVarName = "CLICKHOUSE_USER";
    private const string PasswordEnvVarName = "CLICKHOUSE_PASSWORD";

    /// <summary>
    /// Adds a ClickHouse resource to the application model. A container is used for local development. This version the package defaults to the 23.8.4.69 tag of the clickhouse-server container image
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="userName">The parameter used to provide the user name for the ClickHouse resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the administrator password for the ClickHouse resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port used when launching the container. If null a random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ClickHouseServerResource> AddClickHouse(this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null)
    {
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var server = new ClickHouseServerResource(name, userName?.Resource, passwordParameter);
        return builder.AddResource(server)
                      .WithEndpoint(port: port, targetPort: 8123, name: ClickHouseServerResource.PrimaryEndpointName) // HTTP default port is 8443.
                      .WithImage(ClickHouseContainerImageTags.Image, ClickHouseContainerImageTags.Tag)
                      .WithImageRegistry(ClickHouseContainerImageTags.Registry)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[UserEnvVarName] = server.UserNameReference;
                          context.EnvironmentVariables[PasswordEnvVarName] = server.PasswordParameter;
                      });
    }

    /// <summary>
    /// Adds a ClickHouse database to the application model.
    /// </summary>
    /// <param name="builder">The ClickHouse server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ClickHouseDatabaseResource> AddDatabase(this IResourceBuilder<ClickHouseServerResource> builder, string name, string? databaseName = null)
    {
        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var database = new ClickHouseDatabaseResource(name, databaseName, builder.Resource);
        return builder.ApplicationBuilder.AddResource(database);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a ClickHouse container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ClickHouseServerResource> WithDataVolume(this IResourceBuilder<ClickHouseServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/clickhouse", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a ClickHouse container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ClickHouseServerResource> WithDataBindMount(this IResourceBuilder<ClickHouseServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/var/lib/clickhouse", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the init folder to a ClickHouse container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ClickHouseServerResource> WithInitBindMount(this IResourceBuilder<ClickHouseServerResource> builder, string source, bool isReadOnly = true)
        => builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
}
