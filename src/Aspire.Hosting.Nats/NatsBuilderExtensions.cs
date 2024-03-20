// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding NATS resources to the application model.
/// </summary>
public static class NatsBuilderExtensions
{
    /// <summary>
    /// Adds a NATS server resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for NATS server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> AddNats(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var nats = new NatsServerResource(name);
        return builder.AddResource(nats)
                      .WithEndpoint(containerPort: 4222, hostPort: port, name: NatsServerResource.PrimaryEndpointName)
                      .WithImage("nats", "2");
    }

    /// <summary>
    /// Adds JetStream support to the NATS server resource.
    /// </summary>
    /// <param name="builder">NATS resource builder.</param>
    /// <param name="srcMountPath">Optional mount path providing persistence between restarts.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NatsServerResource> WithJetStream(this IResourceBuilder<NatsServerResource> builder, string? srcMountPath = null)
    {
        var args = new List<string> { "-js" };
        if (srcMountPath != null)
        {
            args.Add("-sd");
            args.Add("/data");
            builder.WithBindMount(srcMountPath, "/data");
        }

        return builder.WithArgs(args.ToArray());
    }
}
