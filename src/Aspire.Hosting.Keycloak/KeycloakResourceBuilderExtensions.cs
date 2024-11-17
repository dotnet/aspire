// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Keycloak;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Keycloak resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class KeycloakResourceBuilderExtensions
{
    private const string AdminEnvVarName = "KEYCLOAK_ADMIN";
    private const string AdminPasswordEnvVarName = "KEYCLOAK_ADMIN_PASSWORD";
    private const int DefaultContainerPort = 8080;
    private const string RealmImportDirectory = "/opt/keycloak/data/import";

    /// <summary>
    /// Adds a Keycloak container to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. </param>
    /// <param name="port">The host port that the underlying container is bound to when running locally.</param>
    /// <param name="adminUsername">The parameter used as the admin for the Keycloak resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="adminPassword">The parameter used as the admin password for the Keycloak resource. If <see langword="null"/> a default password will be used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The container exposes port 8080 by default.
    /// This version of the package defaults to the <inheritdoc cref="KeycloakContainerImageTags.Tag"/> tag of the <inheritdoc cref="KeycloakContainerImageTags.Registry"/>/<inheritdoc cref="KeycloakContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak");
    ///
    /// var myService = builder.AddProject&lt;Projects.MyService&lt;()
    ///                        .WithReference(keycloak);
    /// </code>
    /// </example>
    public static IResourceBuilder<KeycloakResource> AddKeycloak(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? adminUsername = null,
        IResourceBuilder<ParameterResource>? adminPassword = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var passwordParameter = adminPassword?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var resource = new KeycloakResource(name, adminUsername?.Resource, passwordParameter);

        var keycloak = builder
            .AddResource(resource)
            .WithImage(KeycloakContainerImageTags.Image)
            .WithImageRegistry(KeycloakContainerImageTags.Registry)
            .WithImageTag(KeycloakContainerImageTags.Tag)
            .WithHttpEndpoint(port: port, targetPort: DefaultContainerPort)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[AdminEnvVarName] = resource.AdminReference;
                context.EnvironmentVariables[AdminPasswordEnvVarName] = resource.AdminPasswordParameter;
            });

        if (builder.ExecutionContext.IsRunMode)
        {
            keycloak.WithArgs("start-dev");
        }
        else
        {
            keycloak.WithArgs("start");
        }

        keycloak.WithArgs("--import-realm");

        return keycloak;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Keycloak container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The volume is mounted at /opt/keycloak/data in the container.
    /// </remarks>
    /// <example>
    /// Use a data volume
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithDataVolume();
    /// </code>
    /// </example>
    public static IResourceBuilder<KeycloakResource> WithDataVolume(this IResourceBuilder<KeycloakResource> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/opt/keycloak/data",
            false);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Keycloak container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The source directory is mounted at /opt/keycloak/data in the container.
    /// </remarks>
    /// <example>
    /// Use a bind mount
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithDataBindMount("mydata");
    /// </code>
    /// </example>
    public static IResourceBuilder<KeycloakResource> WithDataBindMount(this IResourceBuilder<KeycloakResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/opt/keycloak/data", false);
    }

    /// <summary>
    /// Adds a realm import to a Keycloak container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="importDirectory">The directory containing the realm import files.</param>
    /// <param name="isReadOnly">A flag that indicates if the realm import directory is read-only.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The realm import files are mounted at /opt/keycloak/data/import in the container.
    /// </remarks>
    /// <example>
    /// Import the realms from a directory
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithRealmImport("../realms");
    /// </code>
    /// </example>
    public static IResourceBuilder<KeycloakResource> WithRealmImport(
        this IResourceBuilder<KeycloakResource> builder,
        string importDirectory,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(importDirectory);

        var importDirectoryFullPath = Path.GetFullPath(importDirectory, builder.ApplicationBuilder.AppHostDirectory);
        if (!Directory.Exists(importDirectoryFullPath))
        {
            throw new DirectoryNotFoundException($"The realm import directory '{importDirectoryFullPath}' does not exist.");
        }

        builder.WithBindMount(importDirectoryFullPath, RealmImportDirectory, isReadOnly);

        return builder;
    }
}
