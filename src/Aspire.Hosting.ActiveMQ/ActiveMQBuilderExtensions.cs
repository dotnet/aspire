// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ActiveMQ;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding ActiveMQ resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class ActiveMQBuilderExtensions
{
    /// <summary>
    /// Adds a ActiveMQ container to the application model.
    /// </summary>
    /// <remarks>
    /// The default image and tag are "ActiveMQ" and "3".
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="userName">The parameter used to provide the user name for the ActiveMQ resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the password for the ActiveMQ resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port that the underlying container is bound to when running locally.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ActiveMQServerResource> AddActiveMQ(this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null)
    {
        // don't use special characters in the password, since it goes into a URI
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", special: false);

        var activeMq = new ActiveMQServerResource(name, userName?.Resource, passwordParameter);
        var activemq = builder.AddResource(activeMq)
                              .WithImage(ActiveMQContainerImageTags.Image, ActiveMQContainerImageTags.Tag)
                              .WithImageRegistry(ActiveMQContainerImageTags.Registry)
                              .WithEndpoint(port: port, targetPort: 61616, name: ActiveMQServerResource.PrimaryEndpointName)
                              .WithEnvironment(context =>
                              {
                                  context.EnvironmentVariables["ACTIVEMQ_CONNECTION_USER"] = activeMq.UserNameReference;
                                  context.EnvironmentVariables["ACTIVEMQ_CONNECTION_PASSWORD"] = activeMq.PasswordParameter;
                              });

        return activemq;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a ActiveMQ container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ActiveMQServerResource> WithDataVolume(this IResourceBuilder<ActiveMQServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder
            .WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"),
                "/opt/apache-activemq/data",
                isReadOnly);

    /// <summary>
    /// Adds a named volume for the config folder to a ActiveMQ container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ActiveMQServerResource> WithConfVolume(this IResourceBuilder<ActiveMQServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder
            .WithVolume(name ?? VolumeNameGenerator.Generate(builder, "conf"),
                "/opt/apache-activemq/conf",
                isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a ActiveMQ container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ActiveMQServerResource> WithDataBindMount(this IResourceBuilder<ActiveMQServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source,
            "/opt/apache-activemq/data",
            isReadOnly);

    /// <summary>
    /// Adds a bind mount for the conf folder to a ActiveMQ container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ActiveMQServerResource> WithConfBindMount(this IResourceBuilder<ActiveMQServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source,
            "/opt/apache-activemq/conf",
            isReadOnly);

}
