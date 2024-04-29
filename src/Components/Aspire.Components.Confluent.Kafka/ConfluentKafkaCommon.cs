// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Confluent.Kafka;

internal sealed class ConfluentKafkaCommon
{
    public const string MeterName = "Aspire.Confluent.Kafka";

    public const string ProducerHealthCheckName = "Aspire.Confluent.Kafka.Producer";
    public const string ConsumerHealthCheckName = "Aspire.Confluent.Kafka.Consumer";
    public const string KeyedProducerHealthCheckName = "Aspire.Confluent.Kafka.Producer_";
    public const string KeyedConsumerHealthCheckName = "Aspire.Confluent.Kafka.Consumer_";

    public const string LogCategoryName = "Aspire.Confluent.Kafka";
}
