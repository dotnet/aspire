// Assembly 'Aspire.Confluent.Kafka'

using System;
using Aspire.Confluent.Kafka;
using Confluent.Kafka;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to a Kafka broker.
/// </summary>
public static class AspireKafkaConsumerExtensions
{
    /// <summary>
    /// Registers <see cref="T:Confluent.Kafka.IConsumer`2" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method for customizing the <see cref="T:Aspire.Confluent.Kafka.KafkaConsumerSettings" />.</param>
    /// <param name="configureBuilder">An optional method used for customizing the <see cref="T:Confluent.Kafka.ConsumerBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Consumer" section.</remarks>
    public static void AddKafkaConsumer<TKey, TValue>(this IHostApplicationBuilder builder, string connectionName, Action<KafkaConsumerSettings>? configureSettings = null, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Confluent.Kafka.IConsumer`2" /> as a keyed singleton for the given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method for customizing the <see cref="T:Aspire.Confluent.Kafka.KafkaConsumerSettings" />.</param>
    /// <param name="configureBuilder">An optional method used for customizing the <see cref="T:Confluent.Kafka.ConsumerBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Consumer:{name}" section.</remarks>
    public static void AddKeyedKafkaConsumer<TKey, TValue>(this IHostApplicationBuilder builder, string name, Action<KafkaConsumerSettings>? configureSettings = null, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder = null);
}
