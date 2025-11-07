// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Models a runnable debug configuration for a JavaScript or Node.js application.
/// </summary>
public sealed class JavaScriptLaunchConfiguration() : ExecutableLaunchConfigurationWithDebuggerProperties<JavaScriptDebuggerProperties>("node")
{
    /// <summary>
    /// Provides the fully qualified path to the JavaScript program's entry file.
    /// </summary>
    [JsonPropertyName("program")]
    public string Program { get; set; } = string.Empty;

    /// <summary>
    /// Provides the fully qualified path to the Node.js runtime executable.
    /// </summary>
    [JsonPropertyName("runtimeExecutable")]
    public string? RuntimeExecutable { get; set; }

    /// <summary>
    /// Optional arguments passed to the runtime executable.
    /// </summary>
    [JsonPropertyName("runtimeArgs")]
    public string[]? RuntimeArgs { get; set; }

    /// <summary>
    /// Specifies the version of the runtime to use.
    /// </summary>
    [JsonPropertyName("runtimeVersion")]
    public string? RuntimeVersion { get; set; }
}

/// <summary>
/// Models debugger properties for a JavaScript/Node.js application made available by the Node.js debug adapter.
/// </summary>
public sealed class JavaScriptDebuggerProperties : DebuggerProperties
{
    /// <summary>
    /// Identifies the type of debugger to use.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type { get; set; } = "node";

    /// <summary>
    /// Provides the name for the debug configuration that appears in the VS Code dropdown list.
    /// </summary>
    [JsonPropertyName("name")]
    public override required string Name { get; set; }

    /// <summary>
    /// Provides the fully qualified path to the Node.js runtime executable.
    /// </summary>
    [JsonPropertyName("runtimeExecutable")]
    public string? RuntimeExecutable { get; init; }

    /// <summary>
    /// Optional arguments passed to the runtime executable.
    /// </summary>
    [JsonPropertyName("runtimeArgs")]
    public string[]? RuntimeArgs { get; init; }

    /// <summary>
    /// Specifies the version of the runtime to use.
    /// </summary>
    [JsonPropertyName("runtimeVersion")]
    public string? RuntimeVersion { get; init; }

    /// <summary>
    /// Specifies whether to stop at the entry point of the program.
    /// </summary>
    [JsonPropertyName("stopOnEntry")]
    public bool StopOnEntry { get; set; } = false;

    /// <summary>
    /// Provides the working directory for the program being debugged.
    /// </summary>
    [JsonPropertyName("cwd")]
    public override required string WorkingDirectory { get; init; }

    /// <summary>
    /// Array of glob patterns for locating generated JavaScript files.
    /// </summary>
    [JsonPropertyName("outFiles")]
    public string[]? OutFiles { get; set; }

    /// <summary>
    /// Array of glob patterns for locations where source maps should be parsed.
    /// </summary>
    [JsonPropertyName("resolveSourceMapLocations")]
    public string[]? ResolveSourceMapLocations { get; set; }

    /// <summary>
    /// When restarting a session, give up after this number of milliseconds.
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    /// <summary>
    /// VS Code's root directory for remote debugging.
    /// </summary>
    [JsonPropertyName("localRoot")]
    public string? LocalRoot { get; set; }

    /// <summary>
    /// Node's root directory for remote debugging.
    /// </summary>
    [JsonPropertyName("remoteRoot")]
    public string? RemoteRoot { get; set; }

    /// <summary>
    /// Try to automatically step over code that doesn't map to source files.
    /// </summary>
    [JsonPropertyName("smartStep")]
    public bool? SmartStep { get; set; }

    /// <summary>
    /// Automatically skip files covered by these glob patterns.
    /// </summary>
    [JsonPropertyName("skipFiles")]
    public string[]? SkipFiles { get; set; }

    /// <summary>
    /// Enable diagnostic output.
    /// </summary>
    [JsonPropertyName("trace")]
    public bool? Trace { get; set; }
}
