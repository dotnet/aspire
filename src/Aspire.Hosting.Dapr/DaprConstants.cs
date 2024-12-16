// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        new ("pubsub.azure.eventhubs", "v1"),
        new ("pubsub.azure.servicebus.queues", "v1"),
        new ("pubsub.azure.servicebus.topics", "v1"),
        new ("pubsub.rabbitmq", "v1"),
        new ("bindings.azure.eventhubs", "v1"),
        new ("bindings.azure.servicebusqueues", "v1"),
        new ("bindings.azure.signalr", "v1"),
        new ("bindings.mqtt3", "v1", "url"),
        new ("bindings.mysql", "v1", "url"),
        new ("bindings.postgresql", "v1"),
        new ("state.cockroachdb", "v1"),
        new ("state.sqlserver", "v1"),
        new ("state.mysql", "v1"),
        new ("state.oracledatabase", "v1"),
        new ("state.postgresql", "v2"),
        new ("state.postgresql", "v1"),
        new ("state.sqlite", "v1"),
        new ("configuration.azure.appconfig", "v1"),
        new ("configuration.postgresql", "v1"),
        new ("bindings.azure.signalr", "v1"),
        new ("bindings.azure.signalr", "v1")
    ];

    /// <summary>
    /// Representation of a supported Dapr components,
    /// which supports connectionstrings.
    /// </summary>
    /// <param name="Type">The dapr compnent type</param>
    /// <param name="Version">The version of the component format</param>
    /// <param name="PropertyName">
    /// The property name, inside the component configuration 
    /// where the connection string should be configured
    /// </param>
    public sealed record DaprSupportedRefType(string Type, string Version, string PropertyName = "connectionString");
}
