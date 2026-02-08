// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Watch;

internal sealed class WatchStatusEvent
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("projects")]
    public required string[] Projects { get; init; }

    [JsonPropertyName("success")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Success { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    [JsonPropertyName("exitCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ExitCode { get; init; }

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
