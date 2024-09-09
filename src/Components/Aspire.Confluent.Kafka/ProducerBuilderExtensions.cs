// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Confluent.Kafka;

namespace Aspire.Confluent.Kafka;

internal static class ProducerBuilderExtensions
{
    public static void OverrideConnectionString<TKey, TValue>(this ProducerBuilder<TKey, TValue> builder, string connectionString)
    {
        var configProperty = builder.GetType()
            .GetProperty("Config", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException(
            "The 'Config' property of type 'ProducerBuilder<TKey, TValue>' is not found");

        // Retrieve the reference to the internal Config property and enumerate it to a temporary collection.
        var config = configProperty
            .GetValue(builder) as IEnumerable<KeyValuePair<string, string>> ?? [];

        // Create a new dictionary to hold the updated configuration.
        var updatedConfig = new Dictionary<string, string>(config)
        {
            // Override the connection string.
            ["bootstrap.servers"] = connectionString
        };

        // Set the updated configuration back to the builder.
        configProperty
            .SetValue(builder, updatedConfig);
    }
}
