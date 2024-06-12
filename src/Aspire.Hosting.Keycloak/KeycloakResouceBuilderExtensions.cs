// Licensed to the .NET Foundation under one or more agreements.
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

    /// <summary>
    /// Adds a Keycloak container to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. </param>
    /// <param name="port">The host port that the underlying container is bound to when running locally.</param>
    /// <param name="admin">The parameter used as the admin for the Keycloak resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="adminPassword">The parameter used as the admin password for the Keycloak resource. If <see langword="null"/> a default password will be used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KeycloakResource> AddKeycloak(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? admin = null,
        IResourceBuilder<ParameterResource>? adminPassword = null)
    {
        var passwordParameter = adminPassword?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var resource = new KeycloakResource(name, admin?.Resource, passwordParameter);

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

        return keycloak;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Keycloak container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KeycloakResource> WithDataVolume(this IResourceBuilder<KeycloakResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/opt/keycloak/data", isReadOnly);

    /// <summary>
    /// Adds a reference to a Keycloak resource to the specified resource builder.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource builder.</typeparam>
    /// <param name="builder">The resource builder to add the reference to.</param>
    /// <param name="keycloakBuilder">The Keycloak resource builder to reference.</param>
    /// <param name="realm">The realm to reference in the Keycloak server.</param>
    /// <returns>The resource builder with the added reference.</returns>
    public static IResourceBuilder<TResource> WithReference<TResource>(
        this IResourceBuilder<TResource> builder,
        IResourceBuilder<KeycloakResource> keycloakBuilder,
        string realm
    )
        where TResource : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(keycloakBuilder);

        builder.WithReference(keycloakBuilder)
               .WithEnvironment("Aspire__Keycloak__Realm", realm);

        return builder;
    }
}
