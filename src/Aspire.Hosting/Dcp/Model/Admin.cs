// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Dcp.Model;

// Represents the state of DCP API server execution.
internal sealed class ApiServerExecution
{
    // The current status of the API server.
    [JsonPropertyName("status")]
    public string? ApiServerStatus { get; set; }

    [JsonPropertyName("shutdownResourceCleanup")]
    public string? ShutdownResourceCleanup { get; set; } = ResourceCleanup.Full;
}

internal static class ApiServerStatus
{
    // The server is running (default state).
    public const string Running = "Running";

    // The server is stopping (also used for programmatic server stoppage).
    public const string Stopping = "Stopping";

    // The server has stopped (final state).
    public const string Stopped = "Stopped";
}

internal static class ResourceCleanup
{
    // Full resource cleanup (default).
    // Projects will be stopped, non-persistent containers will be deleted etc.
    public const string Full = "Full";

    public const string None = "None";
}
