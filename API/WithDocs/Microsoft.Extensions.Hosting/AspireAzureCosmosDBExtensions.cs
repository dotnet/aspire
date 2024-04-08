// Assembly 'Aspire.Microsoft.Azure.Cosmos'

using System;
using Aspire.Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Azure CosmosDB extension
/// </summary>
public static class AspireAzureCosmosDBExtensions
{
    /// <summary>
    /// Registers <see cref="T:Microsoft.Azure.Cosmos.CosmosClient" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// Configures logging and telemetry for the <see cref="T:Microsoft.Azure.Cosmos.CosmosClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Microsoft.Azure.Cosmos.AzureCosmosDBSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientOptions">An optional method that can be used for customizing the <see cref="T:Microsoft.Azure.Cosmos.CosmosClientOptions" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Azure:Cosmos" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddAzureCosmosDBClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureCosmosDBSettings>? configureSettings = null, Action<CosmosClientOptions>? configureClientOptions = null);

    /// <summary>
    /// Registers <see cref="T:Microsoft.Azure.Cosmos.CosmosClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// Configures logging and telemetry for the <see cref="T:Microsoft.Azure.Cosmos.CosmosClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Microsoft.Azure.Cosmos.AzureCosmosDBSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientOptions">An optional method that can be used for customizing the <see cref="T:Microsoft.Azure.Cosmos.CosmosClientOptions" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Azure:Cosmos:{name}" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">If required ConnectionString is not provided in configuration section</exception>
    public static void AddKeyedAzureCosmosDbClient(this IHostApplicationBuilder builder, string name, Action<AzureCosmosDBSettings>? configureSettings = null, Action<CosmosClientOptions>? configureClientOptions = null);
}
