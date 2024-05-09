// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Apache.Pulsar;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Pulsar Manager resource to a <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class PulsarManagerBuilderExtensions
{
    /// <summary>
    /// Configures a container resource for Pulsar Manager which is pre-configured to
    /// connect to the <see cref="PulsarResource"/> that this method is used on.
    /// </summary>
    /// <remarks>
    /// The default image and tag are "apachepulsar/pulsar-manager" and "0.4.0".
    /// </remarks>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarResource"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the endpoint name when referenced in dependency.</param>
    /// <param name="frontendPort">The manager frontend port that the underlying container is bound to when running locally.</param>
    /// <param name="backendPort">The manager backend port that the underlying container is bound to when running locally.</param>
    /// <param name="configureContainer">Configuration callback for Pulsar Manager container resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarResource"/>.</returns>
    public static IResourceBuilder<PulsarResource> WithPulsarManager(
        this IResourceBuilder<PulsarResource> builder,
        string? name = null,
        int? frontendPort = null,
        int? backendPort = null,
        Action<IResourceBuilder<PulsarManagerResource>>? configureContainer = null
    )
    {
        var applicationBuilder = builder.ApplicationBuilder;

        if (applicationBuilder.Resources.OfType<PulsarManagerResource>().SingleOrDefault() is not null)
        {
            return builder;
        }

        name ??= $"{builder.Resource.Name}-manager";

        var pulsarManager = new PulsarManagerResource(name);

        var pulsarManagerBuilder = builder.ApplicationBuilder.AddResource(pulsarManager)
            .WithImage(PulsarManagerContainerImageTags.Image, PulsarManagerContainerImageTags.Tag)
            .WithImageRegistry(PulsarManagerContainerImageTags.Registry)
            .WithEndpoint(port: frontendPort, targetPort: 9527, name: PulsarManagerResource.FrontendEndpointName, scheme: "http")
            .WithEndpoint(port: backendPort, targetPort: 7750, name: PulsarManagerResource.BackendEndpointName, scheme: "http");

        pulsarManagerBuilder
            .WithReference(builder.GetEndpoint(PulsarResource.ServiceEndpointName))
            .WithReference(builder.GetEndpoint(PulsarResource.BrokerEndpointName));

        pulsarManagerBuilder
            .WithEnvironment("SPRING_CONFIGURATION_FILE", "/pulsar-manager/pulsar-manager/application.properties");

        configureContainer?.Invoke(pulsarManagerBuilder);

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for the application properties file to a Pulsar Manager.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</returns>
    public static IResourceBuilder<PulsarManagerResource> WithApplicationProperties(
        this IResourceBuilder<PulsarManagerResource> builder,
        string source = "application.properties",
        bool isReadOnly = false
    ) => builder
        .WithBindMount(source, "/pulsar-manager/pulsar-manager/application.properties", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the bookkeeper visual manager configuration file to a Pulsar Manager.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</returns>
    public static IResourceBuilder<PulsarManagerResource> WithBookKeeperVisualManager(
        this IResourceBuilder<PulsarManagerResource> builder,
        string source = "bkvm.conf",
        bool isReadOnly = false
    ) => builder
        .WithBindMount(source, "/pulsar-manager/pulsar-manager/bkvm.conf", isReadOnly);

    /// <summary>
    /// Seeds default super-user to a Pulsar Manager.
    /// </summary>
    /// <remarks>
    /// This method only supports the Pulsar Manager container image and tags above v<c>0.4.0</c><br />
    /// Calling this method on a resource configured with an unrecognized image registry, name, or tag will result in a <see cref="DistributedApplicationException"/> being thrown.<br /><br />
    /// To support seeding super-user on v0.4.0 or lower, please refer to following documentation:<br />
    /// <see href="https://pulsar.apache.org/docs/3.2.x/administration-pulsar-manager/#3-set-the-administrator-account-and-password"/>
    /// </remarks>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</param>
    /// <param name="userName">The parameter used to provide the username for the Pulsar Manager default superuser. If <see langword="null"/> a default value will be used.</param>
    /// <param name="email">The parameter used to provide the email for the Pulsar Manager default superuser. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the password for the Pulsar Manager default superuser. If <see langword="null"/> a random password will be generated.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</returns>
    public static IResourceBuilder<PulsarManagerResource> WithDefaultSuperUser(
        this IResourceBuilder<PulsarManagerResource> builder,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? email = null,
        IResourceBuilder<ParameterResource>? password = null
    )
    {
        var appBuilder = builder.ApplicationBuilder;
        var resourceName = builder.Resource.Name;

        var emailResource = email is not null
            ? ReferenceExpression.Create($"{email.Resource}")
            : ReferenceExpression.Create($"pulsar@manager.com");

        var userNameResource = userName is not null
            ? ReferenceExpression.Create($"{userName.Resource}")
            : ReferenceExpression.Create($"pulsar");

        var passwordResource = password is not null
            ? password.Resource
            : ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
                appBuilder,
              $"{resourceName}-password",
              special: false
            );

        var imageAnnotation = builder.Resource.Annotations.OfType<ContainerImageAnnotation>().Last();

        var supportsSuperuserEnv = PulsarManagerContainerImageTags.SupportsDefaultSuperUserEnvVars(imageAnnotation);
        if (!supportsSuperuserEnv)
        {
            throw new DistributedApplicationException(
                $"Cannot configure the Pulsar Manager resource '{builder.Resource.Name}' to " +
                $"enable the management plugin as it uses an unrecognized container " +
                $"image registry, name, or tag."
            );
        }

        builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["DEFAULT_SUPERUSER_ENABLED"] = "true";
            context.EnvironmentVariables["DEFAULT_SUPERUSER_EMAIL"] = emailResource;
            context.EnvironmentVariables["DEFAULT_SUPERUSER_NAME"] = userNameResource;
            context.EnvironmentVariables["DEFAULT_SUPERUSER_PASSWORD"] = passwordResource;
        });

        return builder;
    }

    /// <summary>
    /// Adds default environment to Pulsar Manager configured with pre-configured Pulsar service and bookie (broker) endpoints.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</param>
    /// <param name="name">Default environment name.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for the <see cref="PulsarManagerResource"/>.</returns>
    public static IResourceBuilder<PulsarManagerResource> WithDefaultEnvironment(
        this IResourceBuilder<PulsarManagerResource> builder,
        string? name = null
    ) => builder
        .WithEnvironment(context =>
        {
            var pulsar = builder.ApplicationBuilder.Resources.OfType<PulsarResource>().Single();
            context.EnvironmentVariables["DEFAULT_ENVIRONMENT_NAME"] = name ?? "default";
            context.EnvironmentVariables["DEFAULT_ENVIRONMENT_BOOKIE_URL"] = pulsar.BrokerEndpoint;
            context.EnvironmentVariables["DEFAULT_ENVIRONMENT_SERVICE_URL"] = pulsar.ServiceEndpoint;
        });
}
