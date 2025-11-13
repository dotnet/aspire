// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Aspire.Hosting;

/// <summary>
/// Models a runnable debug configuration for a .NET project application.
/// </summary>
#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class ProjectLaunchConfiguration() : ExecutableLaunchConfigurationWithDebuggerProperties<CSharpDebuggerProperties>("project")
#pragma warning restore ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
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

    /// <summary>
    /// If you need to stop at the entry point of the target, set this to true.
    /// </summary>
    [JsonPropertyName("stopAtEntry")]
    public bool? StopAtEntry { get; set; }

    /// <summary>
    /// Optionally configure a map of source file paths for when source files are in a different location than when the module was compiled.
    /// Example: { "C:\\foo": "/home/me/foo" }
    /// </summary>
    [JsonPropertyName("sourceFileMap")]
    public Dictionary<string, string>? SourceFileMap { get; set; }

    /// <summary>
    /// You can optionally disable justMyCode by setting it to false. Just My Code is a set of features that makes it easier 
    /// to focus on debugging your code by hiding some of the details of optimized libraries.
    /// </summary>
    [JsonPropertyName("justMyCode")]
    public bool? JustMyCode { get; set; }

    /// <summary>
    /// The debugger requires the pdb and source code to be exactly the same. To change this and disable the requirement 
    /// for sources to be the same, set this to false.
    /// </summary>
    [JsonPropertyName("requireExactSource")]
    public bool? RequireExactSource { get; set; }

    /// <summary>
    /// The debugger steps over properties and operators in managed code by default. To change this and enable stepping 
    /// into properties or operators, set this to false.
    /// </summary>
    [JsonPropertyName("enableStepFiltering")]
    public bool? EnableStepFiltering { get; set; }

    /// <summary>
    /// Configures logging options for the debugger. Can control messages logged to the output window.
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingOptions? Logging { get; set; }

    /// <summary>
    /// Configuration for connecting to a remote computer using another executable to relay standard input and output.
    /// </summary>
    [JsonPropertyName("pipeTransport")]
    public PipeTransportOptions? PipeTransport { get; set; }

    /// <summary>
    /// If true, when an optimized module loads in the target process, the debugger will ask the Just-In-Time compiler 
    /// to generate code with optimizations disabled.
    /// </summary>
    [JsonPropertyName("suppressJITOptimizations")]
    public bool? SuppressJITOptimizations { get; set; }

    /// <summary>
    /// Allows customization of how the debugger searches for symbols (.pdb files).
    /// </summary>
    [JsonPropertyName("symbolOptions")]
    public SymbolOptions? SymbolOptions { get; set; }

    /// <summary>
    /// Allows customization of Source Link behavior by URL. Source Link enables downloading source files 
    /// from URLs embedded in .pdb files.
    /// </summary>
    [JsonPropertyName("sourceLinkOptions")]
    public Dictionary<string, SourceLinkOption>? SourceLinkOptions { get; set; }

    /// <summary>
    /// Specifies the target architecture (x86_64 or arm64). The debugger tries to automatically detect this, 
    /// but you can override the behavior by setting this.
    /// </summary>
    [JsonPropertyName("targetArchitecture")]
    public string? TargetArchitecture { get; set; }

    /// <summary>
    /// Controls if, on launch, the debugger should check if the computer has a self-signed HTTPS certificate 
    /// for developing web projects. If unspecified, defaults to true when serverReadyAction is set.
    /// </summary>
    [JsonPropertyName("checkForDevCert")]
    public bool? CheckForDevCert { get; set; }
}

/// <summary>
/// Configures logging options for the C# debugger.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class LoggingOptions
{
    /// <summary>
    /// If true, the debugger will log exceptions.
    /// </summary>
    [JsonPropertyName("exceptions")]
    public bool? Exceptions { get; set; }

    /// <summary>
    /// If true, the debugger will log module load events.
    /// </summary>
    [JsonPropertyName("moduleLoad")]
    public bool? ModuleLoad { get; set; }

    /// <summary>
    /// If true, the debugger will log program output.
    /// </summary>
    [JsonPropertyName("programOutput")]
    public bool? ProgramOutput { get; set; }

    /// <summary>
    /// If true, the debugger will log browser standard output.
    /// </summary>
    [JsonPropertyName("browserStdOut")]
    public bool? BrowserStdOut { get; set; }

    /// <summary>
    /// If true, the debugger will log console usage messages.
    /// </summary>
    [JsonPropertyName("consoleUsageMessage")]
    public bool? ConsoleUsageMessage { get; set; }

    /// <summary>
    /// Configuration for diagnostic logging to help diagnose debugger problems.
    /// </summary>
    [JsonPropertyName("diagnosticsLog")]
    public DiagnosticsLogOptions? DiagnosticsLog { get; set; }
}

/// <summary>
/// Advanced diagnostic logging options for troubleshooting debugger issues.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class DiagnosticsLogOptions
{
    /// <summary>
    /// The path where diagnostic logs should be written.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Controls the verbosity of diagnostic logging.
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; set; }
}

/// <summary>
/// Configuration for pipe transport to connect to a remote computer.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipeTransportOptions
{
    /// <summary>
    /// The fully qualified path to the pipe program to use (e.g., "ssh").
    /// </summary>
    [JsonPropertyName("pipeProgram")]
    public required string PipeProgram { get; set; }

    /// <summary>
    /// Command line arguments passed to the pipe program.
    /// </summary>
    [JsonPropertyName("pipeArgs")]
    public string[]? PipeArgs { get; set; }

    /// <summary>
    /// The full path to the debugger on the target machine.
    /// </summary>
    [JsonPropertyName("debuggerPath")]
    public string? DebuggerPath { get; set; }

    /// <summary>
    /// The working directory for the pipe program.
    /// </summary>
    [JsonPropertyName("pipeCwd")]
    public string? PipeCwd { get; set; }

    /// <summary>
    /// If true, arguments will be quoted. Defaults to true.
    /// </summary>
    [JsonPropertyName("quoteArgs")]
    public bool? QuoteArgs { get; set; }
}

/// <summary>
/// Configures how the debugger searches for symbol (.pdb) files.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class SymbolOptions
{
    /// <summary>
    /// Array of symbol server URLs or directories to search for .pdb files.
    /// </summary>
    [JsonPropertyName("searchPaths")]
    public string[]? SearchPaths { get; set; }

    /// <summary>
    /// If true, the Microsoft Symbol server (https://msdl.microsoft.com/download/symbols) is added to the search path.
    /// </summary>
    [JsonPropertyName("searchMicrosoftSymbolServer")]
    public bool? SearchMicrosoftSymbolServer { get; set; }

    /// <summary>
    /// If true, the NuGet.org Symbol server (https://symbols.nuget.org/download/symbols) is added to the search path.
    /// </summary>
    [JsonPropertyName("searchNuGetOrgSymbolServer")]
    public bool? SearchNuGetOrgSymbolServer { get; set; }

    /// <summary>
    /// Directory where symbols downloaded from symbol servers should be cached.
    /// </summary>
    [JsonPropertyName("cachePath")]
    public string? CachePath { get; set; }

    /// <summary>
    /// Controls which modules to load symbols for.
    /// </summary>
    [JsonPropertyName("moduleFilter")]
    public ModuleFilter? ModuleFilter { get; set; }
}

/// <summary>
/// Filters which modules should have symbols loaded.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class ModuleFilter
{
    /// <summary>
    /// Either "loadAllButExcluded" or "loadOnlyIncluded".
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// Array of modules to exclude (when mode is "loadAllButExcluded"). Wildcards are supported.
    /// </summary>
    [JsonPropertyName("excludedModules")]
    public string[]? ExcludedModules { get; set; }

    /// <summary>
    /// Array of modules to include (when mode is "loadOnlyIncluded"). Wildcards are supported.
    /// </summary>
    [JsonPropertyName("includedModules")]
    public string[]? IncludedModules { get; set; }

    /// <summary>
    /// If true, for modules not in includedModules, the debugger will still check next to the module itself.
    /// </summary>
    [JsonPropertyName("includeSymbolsNextToModules")]
    public bool? IncludeSymbolsNextToModules { get; set; }
}

/// <summary>
/// Configures Source Link behavior for a specific URL pattern.
/// </summary>
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class SourceLinkOption
{
    /// <summary>
    /// Whether Source Link is enabled for this URL pattern.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
