// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli;

internal class CliSettings
{
    [JsonPropertyName("appHostPath")]
    public string? AppHostPath { get; set; }
}
