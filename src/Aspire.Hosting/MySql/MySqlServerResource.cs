// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The MySQL server root password.</param>
public class MySqlServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the MySQL server root password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the MySQL server.
    /// </summary>
    public string ConnectionStringExpression =>
        $"Server={{{Name}.bindings.tcp.host}};Port={{{Name}.bindings.tcp.port}};User ID=root;Password={{{Name}.inputs.password}}";

    /// <summary>
    /// Gets the connection string for the MySQL server.
    /// </summary>
    /// <returns>A connection string for the MySQL server in the form "Server=host;Port=port;User ID=root;Password=password".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for MySQL.

        var connectionString = $"Server={allocatedEndpoint.Address};Port={allocatedEndpoint.Port};User ID=root;Password=\"{PasswordUtil.EscapePassword(Password)}\"";
        return connectionString;
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
