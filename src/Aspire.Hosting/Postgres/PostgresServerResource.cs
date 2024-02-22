// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a PostgreSQL container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The PostgreSQL server password.</param>
public class PostgresServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the PostgreSQL server password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the PostgreSQL server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return $"Host={{{Name}.bindings.tcp.host}};Port={{{Name}.bindings.tcp.port}};Username=postgres;Password={{{Name}.inputs.password}}";
        }
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL server.
    /// </summary>
    /// <returns>A connection string for the PostgreSQL server in the form "Host=host;Port=port;Username=postgres;Password=password".</returns>
    public string? GetConnectionString()
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionString();
        }

        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for Postgres.

        var connectionString = $"Host={allocatedEndpoint.Address};Port={allocatedEndpoint.Port};Username=postgres;Password={PasswordUtil.EscapePassword(Password)}";
        return connectionString;
    }

    private readonly List<string> _databases = new List<string>();

    /// <summary>
    /// List of databases hosted on this server resource.
    /// </summary>
    public IEnumerable<string> Databases => _databases.ToImmutableArray();

    internal void AddDatabase(string databaseName)
    {
        if (_databases.Contains(databaseName, StringComparers.ResourceName))
        {
            return;
        }

        _databases.Add(databaseName);
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
