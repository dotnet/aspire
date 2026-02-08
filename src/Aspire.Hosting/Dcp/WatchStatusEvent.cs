// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Dcp;

internal sealed class WatchStatusEvent
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("projects")]
    public string[]? Projects { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    public static class Types
    {
        public const string Building = "building";
        public const string BuildComplete = "build_complete";
        public const string HotReloadApplied = "hot_reload_applied";
        public const string Restarting = "restarting";
        public const string ProcessExited = "process_exited";
        public const string ProcessStarted = "process_started";
    }
}
