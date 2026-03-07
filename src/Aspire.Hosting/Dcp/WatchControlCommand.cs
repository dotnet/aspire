// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Dcp;

internal sealed class WatchControlCommand
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("projects")]
    public string[]? Projects { get; set; }

    public static class Types
    {
        public const string Rebuild = "rebuild";
    }
}
