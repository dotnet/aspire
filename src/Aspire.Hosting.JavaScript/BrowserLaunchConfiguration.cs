// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// A generic resource that represents a browser debugger configuration for JavaScript applications.
/// </summary>
/// <typeparam name="T">The type of debugger properties used by this browser debugger resource.</typeparam>
/// <remarks>
/// This resource is created as a child of a JavaScript application resource when browser debugging is enabled.
/// It launches a controlled browser instance that can be debugged using the IDE's browser debugging extension.
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class BrowserDebuggerResource<T> : ExecutableResource
    where T : DebugAdapterProperties
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserDebuggerResource{T}"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="browser">The type of browser to debug (e.g., "msedge", "chrome").</param>
    /// <param name="workingDirectory">The working directory for the debugger.</param>
    /// <param name="debuggerProperties">The debugger properties for the browser.</param>
    public BrowserDebuggerResource(
        string name,
        string browser,
        string workingDirectory,
        T debuggerProperties) : base(name, browser, workingDirectory)
    {
        DebuggerProperties = debuggerProperties;
    }

    /// <summary>
    /// Gets the debugger properties for the browser.
    /// </summary>
    public T DebuggerProperties { get; init; }
}

/// <summary>
/// A VS Code-specific resource that represents a browser debugger configuration for JavaScript applications.
/// </summary>
/// <remarks>
/// This resource is created as a child of a JavaScript application resource when browser debugging is enabled.
/// It launches a controlled browser instance that can be debugged using VS Code's js-debug extension.
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class VSCodeBrowserDebuggerResource : BrowserDebuggerResource<VSCodeBrowserDebuggerProperties>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VSCodeBrowserDebuggerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="browser">The type of browser to debug (e.g., "msedge", "chrome").</param>
    /// <param name="webRoot">The web root directory for the application.</param>
    /// <param name="workingDirectory">The working directory for the debugger.</param>
    /// <param name="url">The URL to launch in the browser.</param>
    /// <param name="configure">An optional action to configure additional debugger properties.</param>
    public VSCodeBrowserDebuggerResource(
        string name,
        string browser,
        string webRoot,
        string workingDirectory,
        string url,
        Action<VSCodeBrowserDebuggerProperties>? configure) : base(name, browser, workingDirectory, CreateDebuggerProperties(name, browser, webRoot, workingDirectory, url, configure))
    {
    }

    private static VSCodeBrowserDebuggerProperties CreateDebuggerProperties(
        string name,
        string browser,
        string webRoot,
        string workingDirectory,
        string url,
        Action<VSCodeBrowserDebuggerProperties>? configure)
    {
        var props = new VSCodeBrowserDebuggerProperties
        {
            Type = browser,
            Name = $"{name} Debugger",
            WebRoot = webRoot,
            Url = url,
            WorkingDirectory = workingDirectory,
            SourceMaps = true,
            // Use a temporary unique user data directory for each browser instance so that
            // multiple browser debuggers can run concurrently without conflicting.
            // The boolean true is a js-debug convention that creates an auto-managed temp directory.
            UserDataDir = true,
            // Allow source maps from anywhere, including webpack dev server
            // js-debug has built-in smart resolution for common bundler patterns
            ResolveSourceMapLocations = ["**", "!**/node_modules/**"],
        };

        configure?.Invoke(props);
        return props;
    }
}

/// <summary>
/// Models a runnable debug configuration for browser-based JavaScript debugging.
/// </summary>
#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public sealed class BrowserLaunchConfiguration() : ExecutableLaunchConfigurationWithDebuggerProperties<DebugAdapterProperties>("browser")
#pragma warning restore ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
}

/// <summary>
/// Models VS Code-specific debugger properties for browser-based JavaScript debugging using the js-debug adapter.
/// </summary>
/// <remarks>
/// These properties map to the VS Code browser debugger configuration options.
/// See https://code.visualstudio.com/docs/nodejs/browser-debugging for more information.
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class VSCodeBrowserDebuggerProperties : VSCodeDebuggerPropertiesBase
{
    /// <summary>
    /// Identifies the type of debugger to use. Defaults to "msedge" for Edge browser debugging.
    /// Other options include "chrome" for Chrome browser debugging.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type { get; set; } = "msedge";

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
    /// The URL to launch in the browser for debugging.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    /// <summary>
    /// The web root directory for the application. Used for resolving source files.
    /// </summary>
    [JsonPropertyName("webRoot")]
    public string? WebRoot { get; set; }

    /// <summary>
    /// Use JavaScript source maps (if they exist). Defaults to true.
    /// </summary>
    [JsonPropertyName("sourceMaps")]
    public bool? SourceMaps { get; set; }

    /// <summary>
    /// A list of glob patterns for locations where source maps should be resolved.
    /// </summary>
    [JsonPropertyName("resolveSourceMapLocations")]
    public string[]? ResolveSourceMapLocations { get; set; }

    /// <summary>
    /// An array of glob patterns for locating generated JavaScript files from source maps.
    /// </summary>
    [JsonPropertyName("outFiles")]
    public string[]? OutFiles { get; set; }

    /// <summary>
    /// A mapping of source file paths to generated file paths for use in source mapping.
    /// </summary>
    [JsonPropertyName("sourceMapPathOverrides")]
    public Dictionary<string, string>? SourceMapPathOverrides { get; set; }

    /// <summary>
    /// An array of glob patterns for files to skip when debugging.
    /// </summary>
    [JsonPropertyName("skipFiles")]
    public string[]? SkipFiles { get; set; }

    /// <summary>
    /// Automatically step through generated code that cannot be mapped back to the original source.
    /// </summary>
    [JsonPropertyName("smartStep")]
    public bool? SmartStep { get; set; }

    /// <summary>
    /// Enables logging of the Debug Adapter Protocol messages between VS Code and the debug adapter.
    /// Can be true, false, or "verbose" for detailed logging.
    /// </summary>
    [JsonPropertyName("trace")]
    public object? Trace { get; set; }

    /// <summary>
    /// The port to use for the browser's remote debugging protocol.
    /// </summary>
    [JsonPropertyName("port")]
    public int? Port { get; set; }

    /// <summary>
    /// Path to the browser executable to use. If not specified, the debugger will try to find one.
    /// </summary>
    [JsonPropertyName("runtimeExecutable")]
    public string? RuntimeExecutable { get; set; }

    /// <summary>
    /// Optional arguments passed to the browser executable.
    /// </summary>
    [JsonPropertyName("runtimeArgs")]
    public string[]? RuntimeArgs { get; set; }

    /// <summary>
    /// The user data directory to use for the browser instance.
    /// Set to <see langword="true"/> (boolean) to auto-create a temporary unique directory,
    /// or a string path to specify a custom directory.
    /// </summary>
    [JsonPropertyName("userDataDir")]
    public object? UserDataDir { get; set; }

    /// <summary>
    /// Path to a file containing environment variables for the browser.
    /// </summary>
    [JsonPropertyName("envFile")]
    public string? EnvFile { get; set; }

    /// <summary>
    /// The timeout in milliseconds to wait for the browser to attach.
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    /// <summary>
    /// The file to open in the browser. Alternative to URL.
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; set; }

    /// <summary>
    /// Path to the server root folder for path mapping.
    /// </summary>
    [JsonPropertyName("pathMapping")]
    public Dictionary<string, string>? PathMapping { get; set; }
}
