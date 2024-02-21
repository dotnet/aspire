// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a RabbitMQ resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The RabbitMQ server password.</param>
public class RabbitMQServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEnvironment
{
    /// <summary>
    /// The RabbitMQ server password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the RabbitMQ server for the manifest.
    /// </summary>
    public string ConnectionStringExpression =>
        $"amqp://guest:{{{Name}.inputs.password}}@{{{Name}.bindings.tcp.host}}:{{{Name}.bindings.tcp.port}}";

    /// <summary>
    /// Gets the connection string for the RabbitMQ server.
    /// </summary>
    /// <returns>A connection string for the RabbitMQ server in the form "amqp://user:password@host:port".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException($"RabbitMQ resource \"{Name}\" does not have endpoint annotation.");
        }

        var endpoint = allocatedEndpoints.Where(a => a.Name != "management").Single();
        return $"amqp://guest:{Password}@{endpoint.EndPointString}";
    }

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.WriteContainer(this);

        context.Writer.WriteStartObject("inputs");      // "inputs": {
        context.Writer.WriteStartObject("password");    //   "password": {
        context.Writer.WriteString("type", "string");   //     "type": "string",
        context.Writer.WriteBoolean("secret", true);    //     "secret": true,
        context.Writer.WriteStartObject("default");     //     "default": {
        context.Writer.WriteStartObject("generate");    //       "generate": {
        context.Writer.WriteNumber("minLength", 10);    //         "minLength": 10,
        context.Writer.WriteEndObject();                //       }
        context.Writer.WriteEndObject();                //     }
        context.Writer.WriteEndObject();                //   }
        context.Writer.WriteEndObject();                // }
    }
}
