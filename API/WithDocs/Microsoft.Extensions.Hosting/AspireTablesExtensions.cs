// Assembly 'Aspire.Azure.Data.Tables'

using System;
using Aspire.Azure.Common;
using Aspire.Azure.Data.Tables;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="T:Azure.Data.Tables.TableServiceClient" /> as a singleton in the services provided by the <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" />.
/// </summary>
public static class AspireTablesExtensions
{
    /// <summary>
    /// Registers <see cref="T:Azure.Data.Tables.TableServiceClient" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Data.Tables.AzureDataTablesSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:Tables" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.ServiceUri" /> is provided.</exception>
    public static void AddAzureTableClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureDataTablesSettings>? configureSettings = null, Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.Data.Tables.TableServiceClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.Data.Tables.AzureDataTablesSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Data:Tables:{name}" section.</remarks>
    /// <exception cref="T:System.InvalidOperationException">Thrown when neither <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.ConnectionString" /> nor <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.ServiceUri" /> is provided.</exception>
    public static void AddKeyedAzureTableClient(this IHostApplicationBuilder builder, string name, Action<AzureDataTablesSettings>? configureSettings = null, Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null);
}
