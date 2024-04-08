// Assembly 'Aspire.MongoDB.Driver'

using System;
using Aspire.MongoDB.Driver;
using MongoDB.Driver;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting MongoDB database with MongoDB.Driver client.
/// </summary>
public static class AspireMongoDBDriverExtensions
{
    /// <summary>
    /// Registers <see cref="T:MongoDB.Driver.IMongoClient" /> and <see cref="T:MongoDB.Driver.IMongoDatabase" /> instances for connecting MongoDB database with MongoDB.Driver client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:MongoDB:Driver" section.</remarks>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing MongoClientSettings.</param>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.MongoDB.Driver.MongoDBSettings.ConnectionString" /> is not provided.</exception>
    public static void AddMongoDBClient(this IHostApplicationBuilder builder, string connectionName, Action<MongoDBSettings>? configureSettings = null, Action<MongoClientSettings>? configureClientSettings = null);

    /// <summary>
    /// Registers <see cref="T:MongoDB.Driver.IMongoClient" /> and <see cref="T:MongoDB.Driver.IMongoDatabase" /> instances for connecting MongoDB database with MongoDB.Driver client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:MongoDB:Driver:{name}" section.</remarks>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing MongoClientSettings.</param>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.MongoDB.Driver.MongoDBSettings.ConnectionString" /> is not provided.</exception>
    public static void AddKeyedMongoDBClient(this IHostApplicationBuilder builder, string name, Action<MongoDBSettings>? configureSettings = null, Action<MongoClientSettings>? configureClientSettings = null);
}
