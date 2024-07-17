﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Keycloak;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Keycloak resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class KeycloakResouceBuilderExtensions
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
    /// The container is based on the quay.io/keycloak/keycloak container image.
    /// The default tag is 24.0. The container exposes port 8080 by default.
    /// </remarks>
    /// <example>
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
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The volume is mounted at /opt/keycloak/data in the container.
    /// </remarks>
    /// <example>
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithDataVolume();
    /// </code>
    /// </example>
    public static IResourceBuilder<KeycloakResource> WithDataVolume(this IResourceBuilder<KeycloakResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/opt/keycloak/data", isReadOnly);

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
        ArgumentNullException.ThrowIfNull(importDirectory);

        if (!Directory.Exists(importDirectory))
        {
            throw new DirectoryNotFoundException($"The realm import directory {importDirectory} does not exist.");
        }

        builder.WithBindMount(importDirectory, RealmImportDirectory, isReadOnly);

        return builder;
    }
}
