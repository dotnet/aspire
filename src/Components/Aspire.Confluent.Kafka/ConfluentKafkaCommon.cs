// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Confluent.Kafka;

internal static class ConfluentKafkaCommon
{
    public const string MeterName = "Aspire.Confluent.Kafka";

    public const string ProducerHealthCheckName = "Aspire.Confluent.Kafka.Producer";
    public const string ConsumerHealthCheckName = "Aspire.Confluent.Kafka.Consumer";
    public const string KeyedProducerHealthCheckName = "Aspire.Confluent.Kafka.Producer_";
    public const string KeyedConsumerHealthCheckName = "Aspire.Confluent.Kafka.Consumer_";

    public const string LogCategoryName = "Aspire.Confluent.Kafka";

    private const string EnableAspire8ConfluentKafkaMetrics = "EnableAspire8ConfluentKafkaMetrics";

    internal static bool IsAspire8ConfluentKafkaMetricsEnabled { get; } =
        AppContext.TryGetSwitch(EnableAspire8ConfluentKafkaMetrics, out var value) && value;
}
