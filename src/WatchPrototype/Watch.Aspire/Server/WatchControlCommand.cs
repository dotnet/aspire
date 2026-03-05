// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Watch;

internal sealed class WatchControlCommand
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Paths of projects to restart.
    /// </summary>
    [JsonPropertyName("projects")]
    public ImmutableArray<string> Projects { get; init; }

    public static class Types
    {
        public const string Rebuild = "rebuild";
    }
}
