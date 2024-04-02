// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.RabbitMQ;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding RabbitMQ resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class RabbitMQBuilderExtensions
{
    /// <summary>
    /// Adds a RabbitMQ container to the application model.
    /// </summary>
    /// <remarks>
    /// The default image and tag are "rabbitmq" and "3".
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="userName">The parameter used to provide the user name for the RabbitMQ resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the password for the RabbitMQ resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port that the underlying container is bound to when running locally.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RabbitMQServerResource> AddRabbitMQ(this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null)
    {
        // don't use special characters in the password, since it goes into a URI
        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", special: false);

        var rabbitMq = new RabbitMQServerResource(name, userName?.Resource, passwordParameter);
        var rabbitmq = builder.AddResource(rabbitMq)
                              .WithImage(RabbitMQContainerImageTags.Image, RabbitMQContainerImageTags.Tag)
                              .WithEndpoint(port: port, targetPort: 5672, name: RabbitMQServerResource.PrimaryEndpointName)
                              .WithEnvironment(context =>
                              {
                                  context.EnvironmentVariables["RABBITMQ_DEFAULT_USER"] = rabbitMq.UserNameReference;
                                  context.EnvironmentVariables["RABBITMQ_DEFAULT_PASS"] = rabbitMq.PasswordParameter;
                              });

        return rabbitmq;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a RabbitMQ container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RabbitMQServerResource> WithDataVolume(this IResourceBuilder<RabbitMQServerResource> builder, string? name = null, bool isReadOnly = false)
        => builder
            .WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/rabbitmq", isReadOnly)
            .RunWithStableNodeName();

    /// <summary>
    /// Adds a bind mount for the data folder to a RabbitMQ container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<RabbitMQServerResource> WithDataBindMount(this IResourceBuilder<RabbitMQServerResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/var/lib/rabbitmq", isReadOnly)
                  .RunWithStableNodeName();

    /// <summary>
    /// Configures the RabbitMQ container resource to enable the RabbitMQ management plugin.
    /// </summary>
    /// <remarks>
    /// This method only supports the default RabbitMQ container image and tags, e.g. <c>3</c>, <c>3.12-alpine</c>, <c>3.12.13-management-alpine</c>, etc.<br />
    /// Calling this method on a resource configured with an unrecognized image registry, name, or tag will result in a <see cref="DistributedApplicationException"/> being thrown.
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="DistributedApplicationException">Thrown when the current container image and tag do not match the defaults for <see cref="RabbitMQServerResource"/>.</exception>
    public static IResourceBuilder<RabbitMQServerResource> WithManagementPlugin(this IResourceBuilder<RabbitMQServerResource> builder)
    {
        var handled = false;
        var containerAnnotations = builder.Resource.Annotations.OfType<ContainerImageAnnotation>().ToList();

        if (containerAnnotations.Count == 1
            && containerAnnotations[0].Registry is null
            && string.Equals(containerAnnotations[0].Image, RabbitMQContainerImageTags.Image, StringComparison.OrdinalIgnoreCase))
        {
            // Existing annotation is in a state we can update to enable the management plugin
            // See tag details at https://hub.docker.com/_/rabbitmq

            const string management = "-management";
            const string alpine = "-alpine";

            var annotation = containerAnnotations[0];
            var existingTag = annotation.Tag;

            if (string.IsNullOrEmpty(existingTag))
            {
                // Set to default tag with management
                annotation.Tag = RabbitMQContainerImageTags.TagManagement;
                handled = true;
            }
            else if (existingTag.EndsWith(management, StringComparison.OrdinalIgnoreCase)
                     || existingTag.EndsWith($"{management}{alpine}", StringComparison.OrdinalIgnoreCase))
            {
                // Already using the management tag
                handled = true;
            }
            else if (existingTag.EndsWith(alpine, StringComparison.OrdinalIgnoreCase)
                     && existingTag.Length > alpine.Length)
            {
                // Transform tag like "3.12-alpine" to "3.12-management-alpine"
                var tagPrefix = existingTag[..existingTag.IndexOf(alpine)];
                annotation.Tag = $"{tagPrefix}{management}{alpine}";
                handled = true;
            }
            else if (IsVersion(existingTag))
            {
                // Tag is in version format so just append "-management"
                annotation.Tag = $"{existingTag}{management}";
                handled = true;
            }
        }

        if (handled)
        {
            builder.WithHttpEndpoint(targetPort: 15672, name: RabbitMQServerResource.ManagementEndpointName);
            return builder;
        }

        throw new DistributedApplicationException($"Cannot configure the RabbitMQ resource '{builder.Resource.Name}' to enable the management plugin as it uses an unrecognized container image registry, name, or tag.");
    }

    private static bool IsVersion(string tag)
    {
        // Must not be empty or null
        if (string.IsNullOrEmpty(tag))
        {
            return false;
        }

        // First char must be a digit
        if (!char.IsAsciiDigit(tag[0]))
        {
            return false;
        }

        // Last char must be digit
        if (!char.IsAsciiDigit(tag[^1]))
        {
            return false;
        }

        // If a single digit no more to check
        if (tag.Length == 1)
        {
            return true;
        }

        // Skip first char as we already checked it's a digit
        var lastCharIsDigit = true;
        for (var i = 1; i < tag.Length; i++)
        {
            var c = tag[i];
            
            if (!(char.IsAsciiDigit(c) || c == '.') // Interim chars must be digits or a period
                || !lastCharIsDigit && c == '.') // '.' can only follow a digit
            {
                return false;
            }

            lastCharIsDigit = char.IsAsciiDigit(c);
        }

        return true;
    }

    private static IResourceBuilder<RabbitMQServerResource> RunWithStableNodeName(this IResourceBuilder<RabbitMQServerResource> builder)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            builder.WithEnvironment(context =>
            {
                // Set a stable node name so queue storage is consistent between sessions
                var nodeName = $"{builder.Resource.Name}@localhost";
                context.EnvironmentVariables["RABBITMQ_NODENAME"] = nodeName;
            });
        }

        return builder;
    }
}
