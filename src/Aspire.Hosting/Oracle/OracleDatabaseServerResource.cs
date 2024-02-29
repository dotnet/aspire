// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Oracle Database container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The Oracle Database server password.</param>
public class OracleDatabaseServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the Oracle Database server password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the Oracle Database server.
    /// </summary>
    public string ConnectionStringExpression =>
        $"user id=system;password={{{Name}.inputs.password}};data source={{{Name}.bindings.tcp.host}}:{{{Name}.bindings.tcp.port}};";

    /// <summary>
    /// Gets the connection string for the Oracle Database server.
    /// </summary>
    /// <returns>A connection string for the Oracle Database server in the form "user id=system;password=password;data source=host:port".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for Oracle.

        var connectionString = $"user id=system;password={PasswordUtil.EscapePassword(Password)};data source={allocatedEndpoint.Address}:{allocatedEndpoint.Port}";
        return connectionString;
    }

    private readonly Dictionary<string, string> _databases = new Dictionary<string, string>(StringComparers.ResourceName);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
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
