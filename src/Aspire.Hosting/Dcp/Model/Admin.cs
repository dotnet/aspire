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

    // Requested resource cleanup type.
    [JsonPropertyName("shutdownResourceCleanup")]
    public string? ShutdownResourceCleanup { get; set; } = ResourceCleanup.Full;

    // Indicates whether the resources have been cleaned up.
    public bool ResourcesCleanedUp =>
        ApiServerStatus is not null && (
        ApiServerStatus == Model.ApiServerStatus.CleanupComplete ||
        ApiServerStatus == Model.ApiServerStatus.Stopping);
}

internal static class ApiServerStatus
{
    // The server is running (default state).
    public const string Running = "Running";

    // The server is stopping/shutting down (also used for programmatic server stoppage).
    // This includes resource cleanup if it was not initiated previously.
    public const string Stopping = "Stopping";

    // The server is in te process of cleaning up resources
    // (also used for triggering the resource cleanup without stopping the server).
    public const string CleaningResources = "CleaningResources";

    // The server completed resource cleanup.
    public const string CleanupComplete = "CleanupComplete";
}

internal static class ResourceCleanup
{
    // Full resource cleanup (default).
    // Projects will be stopped, non-persistent containers will be deleted etc.
    public const string Full = "Full";

    public const string None = "None";
}
