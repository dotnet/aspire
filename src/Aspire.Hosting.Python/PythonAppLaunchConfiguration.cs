// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.Python;

internal sealed class PythonLaunchConfiguration() : ExecutableLaunchConfiguration("python")
{
    [JsonPropertyName("program_path")]
    public string? ProgramPath { get; set; }

    [JsonPropertyName("module")]
    public string? Module { get; set; }
}
