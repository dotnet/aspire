// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Confluent.Kafka.Tests;

internal static class ReflectionHelpers
{
    public static readonly Assembly ComponentAssembly = typeof(AspireKafkaConsumerExtensions).Assembly;
    public static readonly Lazy<Type> MetricsChannelType = new Lazy<Type>(() => ComponentAssembly.GetType("Aspire.Confluent.Kafka.MetricsChannel")!);
    public static readonly Lazy<Type> ProducerConnectionFactoryType = new Lazy<Type>(() => ComponentAssembly.GetType("Aspire.Confluent.Kafka.ProducerConnectionFactory`2")!);
    public static readonly Lazy<Type> ProducerConnectionFactoryStringKeyStringValueType = new Lazy<Type>(() => ProducerConnectionFactoryType.Value.MakeGenericType(typeof(string), typeof(string)));
    public static readonly Lazy<Type> ConsumerConnectionFactoryType = new Lazy<Type>(() => ComponentAssembly.GetType("Aspire.Confluent.Kafka.ConsumerConnectionFactory`2")!);
    public static readonly Lazy<Type> ConsumerConnectionFactoryStringKeyStringValueType = new Lazy<Type>(() => ConsumerConnectionFactoryType.Value.MakeGenericType(typeof(string), typeof(string)));
}
