// Assembly 'Aspire.Confluent.Kafka'

using System;
using Aspire.Confluent.Kafka;
using Confluent.Kafka;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to a Kafka broker.
/// </summary>
public static class AspireKafkaProducerExtensions
{
    /// <summary>
    /// Registers <see cref="T:Confluent.Kafka.IProducer`2" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method used for customizing the <see cref="T:Aspire.Confluent.Kafka.KafkaProducerSettings" />.</param>
    /// <param name="configureBuilder">A method used for customizing the <see cref="T:Confluent.Kafka.ProducerBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Producer" section.</remarks>
    public static void AddKafkaProducer<TKey, TValue>(this IHostApplicationBuilder builder, string connectionName, Action<KafkaProducerSettings>? configureSettings = null, Action<ProducerBuilder<TKey, TValue>>? configureBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Confluent.Kafka.IProducer`2" /> as a keyed singleton for the given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method used for customizing the <see cref="T:Aspire.Confluent.Kafka.KafkaProducerSettings" />.</param>
    /// <param name="configureBuilder">An optional method used for customizing the <see cref="T:Confluent.Kafka.ProducerBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire:Kafka:Producer:{name}" section.</remarks>
    public static void AddKeyedKafkaProducer<TKey, TValue>(this IHostApplicationBuilder builder, string name, Action<KafkaProducerSettings>? configureSettings = null, Action<ProducerBuilder<TKey, TValue>>? configureBuilder = null);
}
