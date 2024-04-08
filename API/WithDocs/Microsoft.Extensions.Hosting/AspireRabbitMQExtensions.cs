// Assembly 'Aspire.RabbitMQ.Client'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aspire.RabbitMQ.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to a RabbitMQ message broker.
/// </summary>
public static class AspireRabbitMQExtensions
{
    /// <summary>
    /// Registers <see cref="T:RabbitMQ.Client.IConnection" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.RabbitMQ.Client.RabbitMQClientSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureConnectionFactory">An optional method that can be used for customizing the <see cref="T:RabbitMQ.Client.ConnectionFactory" />. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:RabbitMQ:Client" section.</remarks>
    public static void AddRabbitMQClient(this IHostApplicationBuilder builder, string connectionName, Action<RabbitMQClientSettings>? configureSettings = null, Action<ConnectionFactory>? configureConnectionFactory = null);

    /// <summary>
    /// Registers <see cref="T:RabbitMQ.Client.IConnection" /> as a keyed singleton for the given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// Enables retries, corresponding health check, logging, and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.RabbitMQ.Client.RabbitMQClientSettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureConnectionFactory">An optional method that can be used for customizing the <see cref="T:RabbitMQ.Client.ConnectionFactory" />. It's invoked after the options are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:RabbitMQ:Client:{name}" section.</remarks>
    public static void AddKeyedRabbitMQClient(this IHostApplicationBuilder builder, string name, Action<RabbitMQClientSettings>? configureSettings = null, Action<ConnectionFactory>? configureConnectionFactory = null);
}
