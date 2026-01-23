// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting;

internal static class ExecutableLaunchMode
{
    public const string Debug = "Debug";
    public const string NoDebug = "NoDebug";
}

/// <summary>
/// Base properties for all executable launch configurations.
/// </summary>
/// <param name="type">Launch configuration type indicator.</param>
public abstract class ExecutableLaunchConfiguration(string type)
{
    /// <summary>
    /// The launch configuration type indicator.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = type;

    /// <summary>
    /// Specifies the launch mode. Currently supported modes are Debug (run the project under the debugger) and NoDebug (run the project without debugging).
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = System.Diagnostics.Debugger.IsAttached ? ExecutableLaunchMode.Debug : ExecutableLaunchMode.NoDebug;
}

/// <summary>
/// Controls the presentation of the debug configuration in the UI.
/// </summary>
public class PresentationOptions
{
    /// <summary>
    /// The order of this item in the debug configuration dropdown.
    /// </summary>
    [JsonPropertyName("order")]
    public int? Order { get; set; }

    /// <summary>
    /// The group this configuration belongs to.
    /// </summary>
    [JsonPropertyName("group")]
    public string? Group { get; set; }

    /// <summary>
    /// Whether this configuration should be hidden from the UI.
    /// </summary>
    [JsonPropertyName("hidden")]
    public bool? Hidden { get; set; }
}

/// <summary>
/// Specifies an action to take when the server is ready.
/// </summary>
public class ServerReadyAction
{
    /// <summary>
    /// The kind of action to take. Currently only "openExternally" is supported.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// The pattern to match in the debug console or integrated terminal output.
    /// </summary>
    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    /// <summary>
    /// The URI format to open. Can include ${port} placeholder.
    /// </summary>
    [JsonPropertyName("uriFormat")]
    public string? UriFormat { get; set; }
}

/// <summary>
/// Base properties for all debuggers. These properties come from https://code.visualstudio.com/docs/debugtest/debugging-configuration, and can
/// be extended to map to the properties made available by any VS Code debug adapter.
/// </summary>
public abstract class VSCodeDebuggerProperties
{
    /// <summary>
    /// The type of debugger to use for this launch configuration.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; set; }

    /// <summary>
    /// The request type of this launch configuration. Currently, launch and attach are supported. Defaults to launch.
    /// </summary>
    [JsonPropertyName("request")]
    public virtual string Request { get; set; } = "launch";

    /// <summary>
    /// The user-friendly name to appear in the Debug launch configuration dropdown.
    /// </summary>
    [JsonPropertyName("name")]
    public abstract string Name { get; set; }

    /// <summary>
    /// The working directory for the program being debugged.
    /// </summary>
    [JsonPropertyName("cwd")]
    public abstract string WorkingDirectory { get; init; }

    /// <summary>
    /// Controls how the debug configuration is displayed in the UI.
    /// </summary>
    [JsonPropertyName("presentation")]
    public PresentationOptions? Presentation { get; set; }

    /// <summary>
    /// The label of a task to launch before the start of a debug session. Can be set to ${defaultBuildTask} to use the default build task.
    /// </summary>
    [JsonPropertyName("preLaunchTask")]
    public string? PreLaunchTask { get; set; }

    /// <summary>
    /// The name of a task to launch at the very end of a debug session.
    /// </summary>
    [JsonPropertyName("postDebugTask")]
    public string? PostDebugTask { get; set; }

    /// <summary>
    /// Controls the visibility of the Debug console panel during a debugging session.
    /// Possible values: "neverOpen", "openOnSessionStart", "openOnFirstSessionStart".
    /// </summary>
    [JsonPropertyName("internalConsoleOptions")]
    public string? InternalConsoleOptions { get; set; }

    /// <summary>
    /// Allows you to connect to a specified port instead of launching the debug adapter.
    /// </summary>
    [JsonPropertyName("debugServer")]
    public int? DebugServer { get; set; }

    /// <summary>
    /// Specifies an action to take when the program outputs a specific message (e.g., opening a URL in a web browser).
    /// </summary>
    [JsonPropertyName("serverReadyAction")]
    public ServerReadyAction? ServerReadyAction { get; set; }
}

/// <summary>
/// Base class for executable launch configurations that include debugger-specific properties.
/// </summary>
/// <typeparam name="T">The type of debugger properties to include.</typeparam>
/// <param name="type">Launch configuration type indicator.</param>
public abstract class ExecutableLaunchConfigurationWithVSCodeDebuggerProperties<T>(string type) : ExecutableLaunchConfiguration(type)
    where T : VSCodeDebuggerProperties
{
    /// <summary>
    /// Debugger-specific properties.
    /// </summary>
    [JsonPropertyName("debugger_properties")]
    public required T DebuggerProperties { get; set; }
}
