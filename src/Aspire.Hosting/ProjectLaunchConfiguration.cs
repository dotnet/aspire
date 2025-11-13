// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting;

/// <summary>
/// Models a runnable debug configuration for a .NET project application.
/// </summary>
public class ProjectLaunchConfiguration() : ExecutableLaunchConfigurationWithDebuggerProperties<CSharpDebuggerProperties>("project")
{
    /// <summary>
    /// The name of the launch profile to be used for project execution.
    /// </summary>
    [JsonPropertyName("launch_profile")]
    public string LaunchProfile { get; set; } = string.Empty;

    /// <summary>
    /// If set to true, the project will be launched without a launch profile and the value of "launch_profile" parameter is disregarded.
    /// </summary>
    [JsonPropertyName("disable_launch_profile")]
    public bool DisableLaunchProfile { get; set; } = false;

    /// <summary>
    /// Path to the project file for the program that is being launched.
    /// </summary>
    [JsonPropertyName("project_path")]
    public string ProjectPath { get; set; } = string.Empty;
}

/// <summary>
/// Models debugger properties for a C# project made available by the coreclr debug adapter.
/// </summary>
public class CSharpDebuggerProperties : DebuggerProperties
{
    /// <summary>
    /// Identifies the type of debugger to use.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type { get; set; } = "coreclr";

    /// <summary>
    /// Provides the name for the debug configuration that appears in the VS Code dropdown list.
    /// </summary>
    [JsonPropertyName("name")]
    public override required string Name { get; set; }

    /// <summary>
    /// Specifies the current working directory for the debugger, which is the base folder for any relative paths used in code.
    /// </summary>
    [JsonPropertyName("cwd")]
    public override required string WorkingDirectory { get; init; }

    // TODO add all other properties supported by the coreclr debug adapter
}
