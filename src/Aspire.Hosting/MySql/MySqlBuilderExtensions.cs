// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MySQL resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MySqlBuilderExtensions
{
    private const string PasswordEnvVarName = "MYSQL_ROOT_PASSWORD";

    /// <summary>
    /// Adds a MySQL container to the application model. The default image is "mysql" and the tag is "latest".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MySQL.</param>
    /// <param name="password">The password for the MySQL root user. Defaults to a random password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MySqlContainerResource}"/>.</returns>
    public static IResourceBuilder<MySqlContainerResource> AddMySqlContainer(this IDistributedApplicationBuilder builder, string name, int? port = null, string? password = null)
    {
        password ??= Guid.NewGuid().ToString("N");
        var mySqlContainer = new MySqlContainerResource(name, password);
        return builder.AddResource(mySqlContainer)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteMySqlContainerToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, port: port, containerPort: 3306)) // Internal port is always 3306.
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mysql", Tag = "latest" })
                      .WithEnvironment(PasswordEnvVarName, () => mySqlContainer.Password);
    }

    /// <summary>
    /// Adds a MySQL connection to the application model. Connection strings can also be read from the connection string section of the configuration using the name of the resource.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The MySQL connection string (optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MySqlConnectionResource}"/>.</returns>
    public static IResourceBuilder<MySqlConnectionResource> AddMySqlConnection(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var mySqlConnection = new MySqlConnectionResource(name, connectionString);

        return builder.AddResource(mySqlConnection)
            .WithAnnotation(new ManifestPublishingCallbackAnnotation((json) => WriteMySqlConnectionToManifest(json, mySqlConnection)));
    }

    /// <summary>
    /// Adds a MySQL database to the application model.
    /// </summary>
    /// <param name="builder">The MySQL server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{MySqlDatabaseResource}"/>.</returns>
    public static IResourceBuilder<MySqlDatabaseResource> AddDatabase(this IResourceBuilder<MySqlContainerResource> builder, string name)
    {
        var mySqlDatabase = new MySqlDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(mySqlDatabase)
                                         .WithAnnotation(new ManifestPublishingCallbackAnnotation(
                                             (json) => WriteMySqlDatabaseToManifest(json, mySqlDatabase)));
    }

    private static void WriteMySqlConnectionToManifest(Utf8JsonWriter jsonWriter, MySqlConnectionResource mySqlConnection)
    {
        jsonWriter.WriteString("type", "mysql.connection.v0");
        jsonWriter.WriteString("connectionString", mySqlConnection.GetConnectionString());
    }

    private static void WriteMySqlContainerToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "mysql.server.v0");
    }

    private static void WriteMySqlDatabaseToManifest(Utf8JsonWriter json, MySqlDatabaseResource mySqlDatabase)
    {
        json.WriteString("type", "mysql.database.v0");
        json.WriteString("parent", mySqlDatabase.Parent.Name);
    }
}
