// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dapr.Models.ComponentSpec;

namespace Aspire.Hosting.Dapr;

internal static class DaprConstants
{
    public static class BuildingBlocks
    {
        public const string PubSub = "pubsub";

        public const string StateStore = "state";
    }

    /// <summary>
    /// List of dapr components, which support connection strings.
    /// </summary>
    public static readonly DaprSupportedRefType[] DaprSupportedRefTypes = [
        new ("pubsub.azure.eventhubs", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("pubsub.azure.servicebus.queues", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("pubsub.azure.servicebus.topics", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("pubsub.rabbitmq", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("bindings.azure.eventhubs", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("bindings.azure.servicebusqueues", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("bindings.azure.signalr", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("bindings.mqtt3", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "url", Value = s} }),
        new ("bindings.mysql", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "url", Value = s} }),
        new ("bindings.postgresql", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.cockroachdb", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.sqlserver", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.mysql", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.oracledatabase", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.postgresql", "v2", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.postgresql", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.sqlite", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("state.redis", "v1", s => {
            var connectionStringParts = s.Split(',');
            var hostname = connectionStringParts.First().Split("://").Last();
            var settings = connectionStringParts.Skip(1).Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
            var result = new List<MetadataValue> {
                new MetadataDirectValue<string>{Name = "redisHost", Value = hostname}
            };
            if(settings.TryGetValue("password", out var password))
            {
                result.Add(new MetadataDirectValue<string> { Name = "redisPassword", Value = password});
            }
            if(settings.TryGetValue("ssl", out var ssl))
            {
                if(ssl == "true")
                {
                    ssl = "true";
                }
                result.Add(new MetadataDirectValue<bool> { Name = "enableTLS", Value = true});
            }
            return result;
        }),
        new ("configuration.azure.appconfig", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("configuration.postgresql", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("configuration.redis", "v1", s => {
            var connectionStringParts = s.Split(',');
            var hostname = connectionStringParts.First().Split("://").Last();
            var settings = connectionStringParts.Skip(1).Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
            var result = new List<MetadataValue> {
                new MetadataDirectValue<string>{Name = "host", Value = hostname}
            };
            if(settings.TryGetValue("password", out var password))
            {
                result.Add(new MetadataDirectValue<string> { Name = "redisPassword", Value = password});
            }
            if(settings.TryGetValue("ssl", out var ssl))
            {
                if(ssl == "true")
                {
                    ssl = "true";
                }
                result.Add(new MetadataDirectValue<bool> { Name = "enableTLS", Value = true});
            }
            return result;
        }),
        new ("bindings.azure.signalr", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("bindings.azure.signalr", "v1", s => new List<MetadataValue> { new MetadataDirectValue<string>{Name = "connectionString", Value = s} }),
        new ("lock.redis", "v1", s => {
            var connectionStringParts = s.Split(',');
            var hostname = connectionStringParts.First().Split("://").Last();
            var settings = connectionStringParts.Skip(1).Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
            var result = new List<MetadataValue> {
                new MetadataDirectValue<string>{Name = "redisHost", Value = hostname}
            };
            if(settings.TryGetValue("password", out var password))
            {
                result.Add(new MetadataDirectValue<string> { Name = "redisPassword", Value = password});
            }
            if(settings.TryGetValue("ssl", out var ssl))
            {
                if(ssl == "true")
                {
                    ssl = "true";
                }
                result.Add(new MetadataDirectValue<bool> { Name = "enableTLS", Value = true});
            }
            return result;
        }),
    ];

    /// <summary>
    /// Representation of a supported Dapr components,
    /// which supports connectionstrings.
    /// </summary>
    /// <param name="Type">The dapr compnent type</param>
    /// <param name="Version">The version of the component format</param>
    /// <param name="propertyMapping">
    /// Defines how the connectionstring sould be represented in the component configuration.
    /// </param>
    public sealed record DaprSupportedRefType(string Type, string Version, Func<string, List<MetadataValue>> propertyMapping);
}
