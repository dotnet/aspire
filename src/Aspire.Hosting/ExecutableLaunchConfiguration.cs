// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting;

/// <summary>
/// Provides constants and utilities for IDE identification and validation.
/// </summary>
/// <remarks>
/// IDEs that support Aspire debugging should set the <see cref="EnvironmentVariableName"/> environment variable
/// to their IDE type constant when launching the app host. This allows Aspire to validate that IDE-specific
/// debug configuration options are only used when the appropriate IDE is connected.
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class AspireIde
{
    /// <summary>
    /// The environment variable name that IDEs should set to identify themselves.
    /// </summary>
    public const string EnvironmentVariableName = "ASPIRE_IDE";

    /// <summary>
    /// IDE type constant for Visual Studio Code.
    /// </summary>
    public const string VSCode = "vscode";

    /// <summary>
    /// Gets the current IDE type from the environment variable.
    /// </summary>
    /// <returns>The IDE type string, or <see langword="null"/> if no IDE has been identified.</returns>
    public static string? GetCurrentIde() => Environment.GetEnvironmentVariable(EnvironmentVariableName);

    /// <summary>
    /// Checks if the current IDE matches the expected IDE type.
    /// </summary>
    /// <param name="expectedIde">The expected IDE type constant (e.g., <see cref="VSCode"/>).</param>
    /// <returns><see langword="true"/> if the current IDE matches; otherwise, <see langword="false"/>.</returns>
    public static bool IsCurrentIde(string expectedIde)
    {
        var currentIde = GetCurrentIde();
        return string.Equals(currentIde, expectedIde, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsVsCode() => IsCurrentIde(VSCode);    
}

/// <summary>
/// Constants for executable launch modes.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class ExecutableLaunchMode
{
    /// <summary>
    /// Run the project under the debugger.
    /// </summary>
    public const string Debug = "Debug";

    /// <summary>
    /// Run the project without debugging.
    /// </summary>
    public const string NoDebug = "NoDebug";
}

/// <summary>
/// Base properties for all executable launch configurations.
/// </summary>
/// <param name="type">Launch configuration type indicator.</param>
[IgnoreNullsOnSerialization]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
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
/// Base properties for all debug adapters following the Debug Adapter Protocol (DAP).
/// These properties represent the standard DAP launch/attach configuration.
/// </summary>
/// <remarks>
/// The actual DAP-standard configuration is determined by the <see cref="Type"/>, <see cref="Request"/>,
/// and (for launch requests) the <see cref="NoDebug"/> property. Everything else is IDE-specific.
/// See <see href="https://microsoft.github.io/debug-adapter-protocol/specification"/> for more information.
/// </remarks>
[JsonConverter(typeof(DebugAdapterPropertiesJsonConverter))]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public abstract class DebugAdapterProperties
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
    /// If <see langword="true"/>, the launch request should launch the program without enabling debugging.
    /// This is an optional DAP property that applies only to launch requests.
    /// </summary>
    [JsonPropertyName("noDebug")]
    public bool? NoDebug { get; set; }

    /// <summary>
    /// The user-friendly name to appear in the Debug launch configuration dropdown.
    /// While not part of the DAP specification, this is commonly used by IDEs.
    /// </summary>
    [JsonPropertyName("name")]
    public abstract string Name { get; set; }

    /// <summary>
    /// The working directory for the program being debugged. While not part of the DAP specification,
    /// this is a common property supported by most debug adapters.
    /// </summary>
    [JsonPropertyName("cwd")]
    public abstract string WorkingDirectory { get; init; }
}

/// <summary>
/// Base class for VS Code-specific debugger properties that includes common VS Code debug configuration options.
/// </summary>
/// <remarks>
/// This class extends <see cref="DebugAdapterProperties"/> with VS Code-specific options from
/// <see href="https://code.visualstudio.com/docs/debugtest/debugging-configuration"/>.
/// All VS Code debugger property classes should inherit from this class.
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public abstract class VSCodeDebuggerPropertiesBase : DebugAdapterProperties
{
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
/// Controls the presentation of the debug configuration in the UI.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
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
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
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
/// Base class for executable launch configurations that include debugger-specific properties.
/// </summary>
/// <typeparam name="T">The type of debugger properties to include.</typeparam>
/// <param name="type">Launch configuration type indicator.</param>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public abstract class ExecutableLaunchConfigurationWithDebuggerProperties<T>(string type) : ExecutableLaunchConfiguration(type)
    where T : DebugAdapterProperties
{
    /// <summary>
    /// Debugger-specific properties. May be null when running in process execution mode without an IDE.
    /// </summary>
    [JsonPropertyName("debugger_properties")]
    public T? DebuggerProperties { get; set; }
}

/// <summary>
/// JSON converter that ensures derived types of <see cref="DebugAdapterProperties"/> are serialized
/// with all their properties, not just the base type properties.
/// </summary>
/// <remarks>
/// This converter enables extensibility by allowing other IDEs to define their own debugger properties
/// classes that inherit from <see cref="DebugAdapterProperties"/> and have all properties serialized correctly.
/// </remarks>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class DebugAdapterPropertiesJsonConverter : JsonConverter<DebugAdapterProperties>
{
    /// <inheritdoc />
    public override DebugAdapterProperties? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // The abstract base type cannot be deserialized into a concrete instance.
        // Skip the JSON value and return null, allowing round-trip through annotation storage.
        reader.Skip();
        return null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DebugAdapterProperties value, JsonSerializerOptions options)
    {
        // Serialize using the actual runtime type to include all derived type properties
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
