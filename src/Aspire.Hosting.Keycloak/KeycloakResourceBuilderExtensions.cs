// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Keycloak;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Keycloak resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class KeycloakResourceBuilderExtensions
{
    private const string AdminEnvVarName = "KC_BOOTSTRAP_ADMIN_USERNAME";
    private const string AdminPasswordEnvVarName = "KC_BOOTSTRAP_ADMIN_PASSWORD";
    private const string HealthCheckEnvVarName = "KC_HEALTH_ENABLED"; // As per https://www.keycloak.org/observability/health
    private const string HttpEnabledEnvVarName = "KC_HTTP_ENABLED";
    private const string ProxyHeadersEnvVarName = "KC_PROXY_HEADERS";
    private const string HostnameEnvVarName = "KC_HOSTNAME";

    private const int DefaultContainerPort = 8080;
    private const int ManagementInterfaceContainerPort = 9000; // As per https://www.keycloak.org/server/management-interface
    private const string ManagementEndpointName = "management";
    private const string KeycloakImportDirectory = "/opt/keycloak/data/import";

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
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak");
    ///
    /// var myService = builder.AddProject&lt;Projects.MyService&lt;()
    ///                        .WithReference(keycloak);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<KeycloakResource> AddKeycloak(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? adminUsername = null,
        IResourceBuilder<ParameterResource>? adminPassword = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var passwordParameter = adminPassword?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var resource = new KeycloakResource(name, adminUsername?.Resource, passwordParameter);

        var keycloak = builder
            .AddResource(resource)
            .WithImage(KeycloakContainerImageTags.Image)
            .WithImageRegistry(KeycloakContainerImageTags.Registry)
            .WithImageTag(KeycloakContainerImageTags.Tag)
            .WithHttpEndpoint(port: port, targetPort: DefaultContainerPort)
            .WithHttpEndpoint(targetPort: ManagementInterfaceContainerPort, name: ManagementEndpointName)
            .WithHttpHealthCheck(endpointName: ManagementEndpointName, path: "/health/ready")
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[AdminEnvVarName] = resource.AdminReference;
                context.EnvironmentVariables[AdminPasswordEnvVarName] = resource.AdminPasswordParameter;
                context.EnvironmentVariables[HealthCheckEnvVarName] = "true";
            })
            .WithUrlForEndpoint(ManagementEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

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
    /// <example>
    /// Use a data volume
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithDataVolume();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<KeycloakResource> WithDataVolume(this IResourceBuilder<KeycloakResource> builder, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/opt/keycloak/data", false);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Keycloak container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The source directory is mounted at /opt/keycloak/data in the container.
    /// <example>
    /// Use a bind mount
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithDataBindMount("mydata");
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<KeycloakResource> WithDataBindMount(this IResourceBuilder<KeycloakResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/opt/keycloak/data", false);
    }

    /// <summary>
    /// Adds a realm import to a Keycloak container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="import">The directory containing the realm import files or a single import file.</param>
    /// <param name="isReadOnly">A flag that indicates if the realm import directory is read-only.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The realm import files are copied to /opt/keycloak/data/import in the container.
    /// <example>
    /// Import the realms from a directory
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithRealmImport("../realms");
    /// </code>
    /// </example>
    /// </remarks>
    [Obsolete("Use WithRealmImport(string import) instead.")]
    public static IResourceBuilder<KeycloakResource> WithRealmImport(
        this IResourceBuilder<KeycloakResource> builder,
        string import,
        bool isReadOnly)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(import);

        var importFullPath = Path.GetFullPath(import, builder.ApplicationBuilder.AppHostDirectory);

        return builder.WithBindMount(importFullPath, KeycloakImportDirectory, isReadOnly);
    }

    /// <summary>
    /// Adds a realm import to a Keycloak container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="import">The directory containing the realm import files or a single import file.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// The realm import files are copied to /opt/keycloak/data/import in the container.
    /// <example>
    /// Import the realms from a directory
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .WithRealmImport("../realms");
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<KeycloakResource> WithRealmImport(
        this IResourceBuilder<KeycloakResource> builder,
        string import)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(import);

        var importFullPath = Path.GetFullPath(import, builder.ApplicationBuilder.AppHostDirectory);

        return builder.WithContainerFiles(
            KeycloakImportDirectory,
            importFullPath,
            defaultOwner: KeycloakContainerImageTags.ContainerUser);
    }

    /// <summary>
    /// Configures Keycloak to run behind a reverse proxy with TLS termination.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="endpointName">The name of the endpoint to use for hostname configuration. If not specified, uses the primary HTTP endpoint.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method configures Keycloak for deployment scenarios where TLS termination is handled by a reverse proxy
    /// (such as Azure Container Apps). It sets the necessary environment variables to enable HTTP communication
    /// and proper proxy header handling.
    /// 
    /// The following Keycloak environment variables are configured:
    /// <list type="bullet">
    /// <item><description><c>KC_HTTP_ENABLED=true</c> - Enables HTTP since the reverse proxy handles TLS termination</description></item>
    /// <item><description><c>KC_PROXY_HEADERS=xforwarded</c> - Configures Keycloak to respect X-Forwarded headers from the reverse proxy</description></item>
    /// <item><description><c>KC_HOSTNAME</c> - Sets the hostname to match the endpoint URL for proper URL generation</description></item>
    /// </list>
    /// 
    /// <example>
    /// Configure Keycloak for deployment behind a reverse proxy
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak");
    /// 
    /// // Use the convenience method that automatically checks the execution context
    /// keycloak.PublishWithReverseProxy();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<KeycloakResource> WithReverseProxy(this IResourceBuilder<KeycloakResource> builder, string? endpointName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEnvironment(context =>
        {
            // Enable HTTP since reverse proxy handles TLS termination
            context.EnvironmentVariables[HttpEnabledEnvVarName] = "true";
            
            // Configure proxy headers for proper client IP and protocol detection
            context.EnvironmentVariables[ProxyHeadersEnvVarName] = "xforwarded";
            
            // Set hostname from endpoint reference for proper URL generation
            var endpointReference = string.IsNullOrEmpty(endpointName) 
                ? builder.GetEndpoint("http") 
                : builder.GetEndpoint(endpointName);
            context.EnvironmentVariables[HostnameEnvVarName] = endpointReference;
        });
    }

    /// <summary>
    /// Configures Keycloak to run behind a reverse proxy with TLS termination when not in run mode.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="endpointName">The name of the endpoint to use for hostname configuration. If not specified, uses the primary HTTP endpoint.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method is a convenience wrapper around <see cref="WithReverseProxy"/> that automatically applies
    /// the reverse proxy configuration only when not in run mode (i.e., during publish operations).
    /// This is useful for scenarios where you want to deploy to environments with reverse proxy support
    /// (such as Azure Container Apps) without affecting local development.
    /// 
    /// <example>
    /// Configure Keycloak for deployment behind a reverse proxy
    /// <code lang="csharp">
    /// var keycloak = builder.AddKeycloak("keycloak")
    ///                       .PublishWithReverseProxy(); // Only applies during publish, not local development
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<KeycloakResource> PublishWithReverseProxy(this IResourceBuilder<KeycloakResource> builder, string? endpointName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            builder.WithReverseProxy(endpointName);
        }

        return builder;
    }
}
