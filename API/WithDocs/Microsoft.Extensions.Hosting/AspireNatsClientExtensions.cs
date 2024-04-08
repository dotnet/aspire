// Assembly 'Aspire.NATS.Net'

using System;
using Aspire.NATS.Net;
using NATS.Client.Core;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting NATS server with NATS client
/// </summary>
public static class AspireNatsClientExtensions
{
    /// <summary>
    /// Registers <see cref="T:NATS.Client.Core.INatsConnection" /> service for connecting NATS server with NATS client.
    /// Configures health check and logging for the NATS client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing NATS options that aren't exposed as standard configuration.</param>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.NATS.Net.NatsClientSettings.ConnectionString" /> is not provided.</exception>
    public static void AddNatsClient(this IHostApplicationBuilder builder, string connectionName, Action<NatsClientSettings>? configureSettings = null, Func<NatsOpts, NatsOpts>? configureOptions = null);

    /// <summary>
    /// Registers <see cref="T:NATS.Client.Core.INatsConnection" /> as a keyed service for given <paramref name="name" /> for connecting NATS server with NATS client.
    /// Configures health check and logging for the NATS client.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureOptions">An optional delegate that can be used for customizing NATS options that aren't exposed as standard configuration.</param>
    /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="builder" /> or <paramref name="name" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">Thrown if mandatory <paramref name="name" /> is empty.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.NATS.Net.NatsClientSettings.ConnectionString" /> is not provided.</exception>
    public static void AddKeyedNatsClient(this IHostApplicationBuilder builder, string name, Action<NatsClientSettings>? configureSettings = null, Func<NatsOpts, NatsOpts>? configureOptions = null);

    /// <summary>
    /// Registers <see cref="T:NATS.Client.JetStream.INatsJSContext" /> service for NATS JetStream operations.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    public static void AddNatsJetStream(this IHostApplicationBuilder builder);
}
