// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Models a runnable debug configuration for a Node.js/TypeScript application.
/// </summary>
#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public sealed class NodeLaunchConfiguration() : ExecutableLaunchConfigurationWithDebuggerProperties<DebugAdapterProperties>("node")
#pragma warning restore ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    /// <summary>
    /// Provides the path to the Node.js script to run.
    /// </summary>
    [JsonPropertyName("script_path")]
    public string ScriptPath { get; set; } = string.Empty;

    /// <summary>
    /// Provides the path to the Node.js runtime executable.
    /// </summary>
    [JsonPropertyName("runtime_executable")]
    public string RuntimeExecutable { get; set; } = string.Empty;
}

/// <summary>
/// Models VS Code-specific debugger properties for a Node.js/TypeScript application made available by the VS Code js-debug adapter.
/// </summary>
/// <remarks>
/// These properties map to the VS Code Node.js debugger configuration options.
/// See https://code.visualstudio.com/docs/nodejs/nodejs-debugging for more information.
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class VSCodeNodeDebuggerProperties : VSCodeDebuggerPropertiesBase
{
    /// <summary>
    /// Identifies the type of debugger to use. Defaults to "node" which uses the built-in js-debug.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type { get; set; } = "node";

    /// <summary>
    /// Provides the name for the debug configuration that appears in the VS Code dropdown list.
    /// </summary>
    [JsonPropertyName("name")]
    public override required string Name { get; set; }

    /// <summary>
    /// Specifies the current working directory for the debugger.
    /// </summary>
    [JsonPropertyName("cwd")]
    public override required string WorkingDirectory { get; init; }

    /// <summary>
    /// Absolute path to the program to debug. This is the entry point script.
    /// </summary>
    [JsonPropertyName("program")]
    public string? Program { get; set; }

    /// <summary>
    /// Runtime to use. Either an absolute path or the name of a runtime available on the PATH.
    /// Defaults to "node".
    /// </summary>
    [JsonPropertyName("runtimeExecutable")]
    public string? RuntimeExecutable { get; set; }

    /// <summary>
    /// Optional arguments passed to the runtime executable.
    /// </summary>
    [JsonPropertyName("runtimeArgs")]
    public string[]? RuntimeArgs { get; set; }

    /// <summary>
    /// When set to true, breaks the debugger at the first line of the program.
    /// </summary>
    [JsonPropertyName("stopOnEntry")]
    public bool? StopOnEntry { get; set; }

    /// <summary>
    /// Specifies how program output is displayed.
    /// Valid values: "internalConsole", "integratedTerminal", "externalTerminal".
    /// </summary>
    [JsonPropertyName("console")]
    public string Console { get; set; } = "internalConsole";

    /// <summary>
    /// An array of glob patterns for files to skip when debugging.
    /// The pattern &lt;node_internals&gt;/** skips Node.js internal modules.
    /// </summary>
    [JsonPropertyName("skipFiles")]
    public string[]? SkipFiles { get; set; }

    /// <summary>
    /// Use JavaScript source maps (if they exist). Defaults to true.
    /// </summary>
    [JsonPropertyName("sourceMaps")]
    public bool? SourceMaps { get; set; }

    /// <summary>
    /// An array of glob patterns for locating generated JavaScript files from source maps.
    /// </summary>
    [JsonPropertyName("outFiles")]
    public string[]? OutFiles { get; set; }

    /// <summary>
    /// Automatically step through generated code that cannot be mapped back to the original source.
    /// </summary>
    [JsonPropertyName("smartStep")]
    public bool? SmartStep { get; set; }

    /// <summary>
    /// Retry connecting to Node.js after this number of milliseconds. Useful for slow-starting programs.
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    /// <summary>
    /// Restart the session when the debugged program exits.
    /// </summary>
    [JsonPropertyName("restart")]
    public bool? Restart { get; set; }

    /// <summary>
    /// Track all subprocesses of the program and debug them too.
    /// </summary>
    [JsonPropertyName("autoAttachChildProcesses")]
    public bool? AutoAttachChildProcesses { get; set; }

    /// <summary>
    /// A list of glob patterns for locations where source maps should be resolved.
    /// </summary>
    [JsonPropertyName("resolveSourceMapLocations")]
    public string[]? ResolveSourceMapLocations { get; set; }

    /// <summary>
    /// Path to an environment variable definitions file (.env file) to load.
    /// </summary>
    [JsonPropertyName("envFile")]
    public string? EnvFile { get; set; }

    /// <summary>
    /// Enables logging of the Debug Adapter Protocol messages between VS Code and the debug adapter.
    /// </summary>
    [JsonPropertyName("trace")]
    public bool? Trace { get; set; }

    /// <summary>
    /// The address of the host to connect to for remote debugging.
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// The port to use for remote debugging.
    /// </summary>
    [JsonPropertyName("port")]
    public int? Port { get; set; }

    /// <summary>
    /// When debugging TypeScript, generate source maps on the fly.
    /// This requires ts-node to be installed in the project.
    /// </summary>
    [JsonPropertyName("runtimeSourcemapPausePatterns")]
    public string[]? RuntimeSourcemapPausePatterns { get; set; }

    /// <summary>
    /// Locations where source maps can be found. Useful when source maps are not next to their source files.
    /// </summary>
    [JsonPropertyName("sourceMapPathOverrides")]
    public Dictionary<string, string>? SourceMapPathOverrides { get; set; }
}
