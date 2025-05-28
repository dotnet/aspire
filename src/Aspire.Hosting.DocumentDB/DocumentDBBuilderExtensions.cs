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
    // Internal port is always 27017.
    private const int DefaultContainerPort = 10260;

    private const string UserEnvVarName = "DOCUMENTDB_INITDB_ROOT_USERNAME";
    private const string PasswordEnvVarName = "DOCUMENTDB_INITDB_ROOT_PASSWORD";

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
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> AddDocumentDB(this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", special: false);

        var DocumentDBContainer = new DocumentDBServerResource(name, userName?.Resource, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(DocumentDBContainer, async (@event, ct) =>
        {
            connectionString = await DocumentDBContainer.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{DocumentDBContainer.Name}' resource but the connection string was null.");
            }
        });

        // var healthCheckKey = $"{name}_check";
        // // cache the client so it is reused on subsequent calls to the health check
        // IMongoClient? client = null;
        // builder.Services.AddHealthChecks()
        //     .AddDocumentDB(
        //         sp => client ??= new MongoClient(connectionString ?? throw new InvalidOperationException("Connection string is unavailable")),
        //         name: healthCheckKey);

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
            }
        });

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
    /// Adds a named volume for the data folder to a DocumentDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> WithDataVolume(this IResourceBuilder<DocumentDBServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data/db", isReadOnly);
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

        return builder.WithBindMount(source, "/data/db", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the init folder to a DocumentDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("Use WithInitFiles instead.")]
    public static IResourceBuilder<DocumentDBServerResource> WithInitBindMount(this IResourceBuilder<DocumentDBServerResource> builder, string source, bool isReadOnly = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
    }

    /// <summary>
    /// Copies init files into a DocumentDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source file or directory on the host to copy into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DocumentDBServerResource> WithInitFiles(this IResourceBuilder<DocumentDBServerResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        const string initPath = "/docker-entrypoint-initdb.d";

        var importFullPath = Path.GetFullPath(source, builder.ApplicationBuilder.AppHostDirectory);

        return builder.WithContainerFiles(
            initPath,
            ContainerDirectory.GetFileSystemItemsFromPath(importFullPath));
    }
}
