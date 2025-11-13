// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.Python;

/// <summary>
/// Models a runnable debug configuration for a python application.
/// </summary>
public sealed class PythonLaunchConfiguration() : ExecutableLaunchConfigurationWithDebuggerProperties<PythonDebuggerProperties>("python")
{
    /// <summary>
    /// Provides the fully qualified path to the python program's entry module (startup file).
    /// <remarks>
    /// <see cref="ProgramPath"/> is mutually exclusive with the <see cref="Module"/> property. One of the two must be provided.
    /// </remarks>
    /// </summary>
    [JsonPropertyName("program_path")]
    public string ProgramPath { get; set; } = string.Empty;

    /// <summary>
    /// Provides the ability to specify the name of a module to be debugged, similarly to the -m argument when run at the command line.
    /// <remarks>
    /// <see cref="Module"/> is mutually exclusive with the <see cref="ProgramPath"/> property. One of the two must be provided.
    /// </remarks>
    /// </summary>
    [JsonPropertyName("module")]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Provides the fully qualified path to the python interpreter to be used to launch the application.
    /// </summary>
    [JsonPropertyName("interpreter_path")]
    public string InterpreterPath { get; set; } = string.Empty;
}

/// <summary>
/// Models debugger properties for a python application made available by the debugpy debug adapter.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PythonDebuggerProperties : DebuggerProperties
{
    /// <summary>
    /// Identifies the type of debugger to use.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type { get; set; } = "debugpy";

    /// <summary>
    /// Provides the name for the debug configuration that appears in the VS Code dropdown list.
    /// </summary>
    [JsonPropertyName("name")]
    public override required string Name { get; set; }

    /// <summary>
    /// Provides the fully qualified path to the python interpreter to be used to launch the application.
    /// </summary>
    [JsonPropertyName("python")]
    public required string InterpreterPath { get; init; }

    /// <summary>
    /// Specifies arguments to pass to the Python interpreter.
    /// </summary>
    [JsonPropertyName("pythonArgs")]
    public string[]? PythonArgs { get; set; }

    /// <summary>
    /// When set to true, activates debugging features specific to the Jinja templating framework.
    /// </summary>
    [JsonPropertyName("jinja")]
    public bool Jinja { get; set; } = true;

    /// <summary>
    /// Provides the fully qualified path to the python program's entry module (startup file).
    /// <remarks>
    /// <see cref="ProgramPath"/> is mutually exclusive with the <see cref="Module"/> property. One of the two must be provided.
    /// </remarks>
    /// </summary>
    [JsonPropertyName("program")]
    public string? ProgramPath { get; init; }

    /// <summary>
    /// Provides the ability to specify the name of a module to be debugged, similarly to the -m argument when run at the command line.
    /// <remarks>
    /// <see cref="Module"/> is mutually exclusive with the <see cref="ProgramPath"/> property. One of the two must be provided.
    /// </remarks>
    /// </summary>
    [JsonPropertyName("module")]
    public string? Module { get; init; }

    /// <summary>
    /// When set to true, breaks the debugger at the first line of the program being debugged. If omitted (the default) or set to false, the debugger runs the program to the first breakpoint.
    /// </summary>
    [JsonPropertyName("stopOnEntry")]
    public bool StopOnEntry { get; set; } = false;

    /// <summary>
    /// When omitted or set to true (the default), restricts debugging to user-written code only. Set to false to also enable debugging of standard library functions.
    /// </summary>
    [JsonPropertyName("justMyCode")]
    public bool JustMyCode { get; set; } = false;

    /// <summary>
    /// Specifies the current working directory for the debugger, which is the base folder for any relative paths used in code.
    /// </summary>
    [JsonPropertyName("cwd")]
    public override required string WorkingDirectory { get; init; }

    /// <summary>
    /// Specifies how program output is displayed.
    /// Due to current limitations with the VS Code implementation, only a value of internalConsole is supported.
    /// </summary>
    [JsonPropertyName("console")]
    public string Console { get; } = "internalConsole";

    /// <summary>
    /// When set to true, activates debugging features specific to the Django web framework.
    /// </summary>
    [JsonPropertyName("django")]
    public bool? Django { get; set; }

    /// <summary>
    /// If set to true, enables debugging of gevent monkey-patched code.
    /// </summary>
    [JsonPropertyName("gevent")]
    public bool? Gevent { get; set; }

    /// <summary>
    /// There is more than one way to configure the Run button, using the purpose option. Setting the option to debug-test, defines that the configuration should be used when debugging tests in VS Code. However, setting the option to debug-in-terminal, defines that the configuration should only be used when accessing the Run Python File button on the top-right of the editor (regardless of whether the Run Python File or Debug Python File options the button provides is used).
    /// Note: The purpose option can't be used to start the debugger through F5 or Run > Start Debugging.
    /// <remarks>
    /// This property is only applicable to VS Code.
    /// </remarks>
    /// </summary>
    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Allows for the automatic reload of the debugger when changes are made to code after the debugger execution has hit a breakpoint
    /// </summary>
    [JsonPropertyName("autoReload")]
    public PythonAutoReloadOptions? AutoReload { get; set; }
}

/// <summary>
/// Models options for automatic reloading of the debugger in a python application.
/// </summary>
public sealed class PythonAutoReloadOptions
{
    /// <summary>
    /// When set to true, enables automatic reloading of the debugger when changes are made to code after the debugger execution has hit a breakpoint.
    /// </summary>
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }
}
