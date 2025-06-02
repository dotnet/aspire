// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DocumentDB;
// using Microsoft.Extensions.DependencyInjection;
// using MongoDB.Driver;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding DocumentDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DocumentDBBuilderExtensions
{
    // default internal port is 10260.
    private const int DefaultContainerPort = 10260;

    private const string UserEnvVarName = "USERNAME";
    private const string PasswordEnvVarName = "PASSWORD";

    /// <summary>
    /// Adds a DocumentDB resource to the application model. A container is used for local development.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="DocumentDBContainerImageTags.Tag"/> tag of the <inheritdoc cref="DocumentDBContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for DocumentDB.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> AddDocumentDB(this IDistributedApplicationBuilder builder, [ResourceName] string name, int? port)
    {
        return AddDocumentDB(builder, name, port, null, null);
    }

    /// <summary>
    /// <inheritdoc cref="AddDocumentDB(IDistributedApplicationBuilder, string, int?)"/>
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for DocumentDB.</param>
    /// <param name="userName">A parameter that contains the DocumentDB server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the DocumentDB server password, or <see langword="null"/> to use a generated password.</param>
    /// <param name="tls">A flag that indicates if TLS should be used for the connection.</param>
    /// <param name="allowInsecureTls">A flag that indicates if invalid TLS certificates should be accepted.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> AddDocumentDB(this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        bool tls = false,
        bool allowInsecureTls = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", special: false);

        var DocumentDBContainer = new DocumentDBServerResource(name, userName?.Resource, passwordParameter, tls, allowInsecureTls);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(DocumentDBContainer, async (@event, ct) =>
        {
            connectionString = await DocumentDBContainer.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{DocumentDBContainer.Name}' resource but the connection string was null.");
            }
        });

        return builder
            .AddResource(DocumentDBContainer)
            .WithEndpoint(port: port, targetPort: DefaultContainerPort, name: DocumentDBServerResource.PrimaryEndpointName)
            .WithImage(DocumentDBContainerImageTags.Image, DocumentDBContainerImageTags.Tag)
            .WithImageRegistry(DocumentDBContainerImageTags.Registry)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[UserEnvVarName] = DocumentDBContainer.UserNameReference;
                context.EnvironmentVariables[PasswordEnvVarName] = DocumentDBContainer.PasswordParameter!;
            });
            //.WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a DocumentDB database to the application model.
    /// </summary>
    /// <param name="builder">The DocumentDB server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBDatabaseResource> AddDatabase(this IResourceBuilder<DocumentDBServerResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var DocumentDBDatabase = new DocumentDBDatabaseResource(name, databaseName, builder.Resource);

        string? connectionString = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(DocumentDBDatabase, async (@event, ct) =>
        {
            connectionString = await DocumentDBDatabase.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{DocumentDBDatabase.Name}' resource but the connection string was null.");
            }        });

        // var healthCheckKey = $"{name}_check";
        // // cache the database client so it is reused on subsequent calls to the health check
        // IMongoDatabase? database = null;
        // builder.ApplicationBuilder.Services.AddHealthChecks()
        //     .AddDocumentDB(
        //         sp => database ??=
        //             new MongoClient(connectionString ?? throw new InvalidOperationException("Connection string is unavailable"))
        //                 .GetDatabase(databaseName),
        //         name: healthCheckKey);

        return builder.ApplicationBuilder
            .AddResource(DocumentDBDatabase);
    }

    /// <summary>
    /// Configures the host port that the DocumentDB resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for DocumentDB.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> WithHostPort(this IResourceBuilder<DocumentDBServerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a named volume for the data folder to a DocumentDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <param name="targetPath">The target path inside the container. Defaults to /home/documentdb/postgresql/data.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> WithDataVolume(
        this IResourceBuilder<DocumentDBServerResource> builder,
        string? name = null,
        bool isReadOnly = false,
        string? targetPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        targetPath ??= "/home/documentdb/postgresql/data";

        return builder
            .WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), targetPath, isReadOnly)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["DATA_PATH"] = targetPath;
            });
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a DocumentDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> WithDataBindMount(this IResourceBuilder<DocumentDBServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        const string targetPath = "/home/documentdb/postgresql/data";

        return builder
            .WithBindMount(source, targetPath, isReadOnly)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["DATA_PATH"] = targetPath;
            });
    }
}
