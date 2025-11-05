// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.Python;

internal sealed class PythonLaunchConfiguration() : ExecutableLaunchConfigurationWithDebuggerProperties<PythonDebuggerProperties>("python")
{
    [JsonPropertyName("program_path")]
    public string ProgramPath { get; set; } = string.Empty;

    [JsonPropertyName("module")]
    public string Module { get; set; } = string.Empty;

    [JsonPropertyName("interpreter_path")]
    public string InterpreterPath { get; set; } = string.Empty;
}

internal sealed class PythonDebuggerProperties : DebuggerProperties
{
    [JsonPropertyName("type")]
    public override string Type { get; init; } = "debugpy";

    [JsonPropertyName("name")]
    public override required string Name { get; init; }

    [JsonPropertyName("python")]
    public required string InterpreterPath { get; init; }

    [JsonPropertyName("jinja")]
    public bool Jinja { get; init; } = true;

    [JsonPropertyName("program")]
    public string? ProgramPath { get; init; }

    [JsonPropertyName("module")]
    public string? Module { get; init; }

    [JsonPropertyName("stopAtEntry")]
    public bool StopAtEntry { get; init; } = false;

    [JsonPropertyName("justMyCode")]
    public bool JustMyCode { get; init; } = false;

    [JsonPropertyName("cwd")]
    public override required string WorkingDirectory { get; init; }
}
