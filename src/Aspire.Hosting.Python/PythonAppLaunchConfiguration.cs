// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.Python;

/// <summary>
/// Represents the VS Code launch configuration for debugging Python applications.
/// </summary>
/// <remarks>
/// <para>
/// This class generates the configuration needed for the VS Code Python debugger extension
/// to attach to and debug a Python application running in Aspire. It extends the base
/// executable launch configuration with Python-specific settings.
/// </para>
/// <para>
/// The configuration is automatically generated when using the Aspire developer dashboard
/// and enables step-through debugging of Python code in VS Code.
/// </para>
/// </remarks>
internal sealed class PythonLaunchConfiguration() : ExecutableLaunchConfiguration("python")
{
    /// <summary>
    /// Gets or sets the path to the Python program (script) to debug.
    /// </summary>
    /// <value>
    /// The absolute path to the Python script file that serves as the entry point for the application.
    /// This corresponds to the "program" setting in VS Code's Python debug configuration.
    /// </value>
    [JsonPropertyName("program_path")]
    public string ProgramPath { get; set; } = string.Empty;
}
