// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Model;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using k8s.Models;

internal sealed class ExecutableSpec
{
    /// <summary>
    /// Path to Executable binary
    /// </summary>
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// The working directory for the Executable
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Launch arguments to be passed to the Executable
    /// </summary>
    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    /// <summary>
    /// Environment variables to be set for the Executable
    /// </summary>
    [JsonPropertyName("env")]
    public List<EnvVar>? Env { get; set; }

    /// <summary>
    /// Environment files to use to populate Executable environment during startup.
    /// </summary>
    [JsonPropertyName("envFiles")]
    public List<string>? EnvFiles { get; set; }

    /// <summary>
    /// The execution type for the Executable
    /// </summary>
    [JsonPropertyName("executionType")]
    public string? ExecutionType { get; set; }

    /// <summary>
    /// Health probes to be run for the Executable.
    /// </summary>
    [JsonPropertyName("healthProbes")]
    public List<HealthProbe>? HealthProbes { get; set; }
}

internal static class ExecutionType
{
    /// <summary>
    /// Executable will be run directly by the controller, as a child process
    /// </summary>
    public const string Process = "Process";

    /// <summary>
    /// Executable will be run via an IDE such as Visual Studio or Visual Studio Code.
    /// </summary>
    public const string IDE = "IDE";
}

internal sealed class ExecutableStatus : V1Status
{
    /// <summary>
    /// The execution ID is the identifier for the actual-state counterpart of the Executable.
    /// For ExecutionType == Process it is the process ID. Process IDs will be eventually reused by OS,
    /// but a combination of process ID and startup timestamp is unique for each Executable instance.
    /// For ExecutionType == IDE it is the IDE session ID.
    /// </summary>
    [JsonPropertyName("executionID")]
    public string? ExecutionID { get; set; }

    /// <summary>
    /// The process ID of the Executable.
    /// </summary>
    [JsonPropertyName("pid")]
    public int ProcessId { get; set; }

    /// <summary>
    /// The current state of the process/IDE session started for this executable
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; } = ExecutableState.Unknown;

    /// <summary>
    /// Start (attempt) timestamp.
    /// </summary>
    [JsonPropertyName("startupTimestamp")]
    public DateTimeOffset? StartupTimestamp { get; set; }

    /// <summary>
    /// The time when the replica finished execution
    /// </summary>
    [JsonPropertyName("finishTimestamp")]
    public DateTimeOffset? FinishTimestamp { get; set; }

    /// <summary>
    /// Exit code of the process associated with the Executable.
    /// The value is equal to UnknownExitCode if the Executable was not started, is still running, or the exit code is not available.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    /// <summary>
    /// The path of a temporary file that contains captured standard output data from the Executable process.
    /// </summary>
    [JsonPropertyName("stdOutFile")]
    public string? StdOutFile { get; set; }

    /// <summary>
    /// The path of a temporary file that contains captured standard error data from the Executable process.
    /// </summary>
    [JsonPropertyName("stdErrFile")]
    public string? StdErrFile { get; set; }

    /// <summary>
    /// Effective values of environment variables, after all substitutions have been applied
    /// </summary>
    [JsonPropertyName("effectiveEnv")]
    public List<EnvVar>? EffectiveEnv { get; set; }

    /// <summary>
    /// Effective values of launch arguments to be passed to the Executable, after all substitutions are applied.
    /// </summary>
    [JsonPropertyName("effectiveArgs")]
    public List<string>? EffectiveArgs { get; set; }

    /// <summary>
    /// The health status of the Executable <see cref="HealthStatus"/> for allowed values.
    /// </summary>
    [JsonPropertyName("healthStatus")]
    public string? HealthStatus { get; set; }

    /// <summary>
    /// Latest results for health probes configured for the Executable.
    /// </summary>
    [JsonPropertyName("healthProbeResults")]
    public List<HealthProbeResult>? HealthProbeResults { get; set;}
}

internal static class ExecutableState
{
    /// <summary>
    /// Executable was successfully started and was running last time we checked.
    /// </summary>
    public const string Running = "Running";

    /// <summary>
    /// Terminated means the Executable was killed by the controller (e.g. as a result of scale-down, or object deletion).
    /// </summary>
    public const string Terminated = "Terminated";

    /// <summary>
    /// Failed to start means the Executable could not be started (e.g. because of invalid path to program file).
    /// </summary>
    public const string FailedToStart = "FailedToStart";

    /// <summary>
    /// Finished means the Executable ran to completion.
    /// </summary>
    public const string Finished = "Finished";

    /// <summary>
    /// Unknown means we are not tracking the actual-state counterpart of the Executable (process or IDE run session).
    /// As a result, we do not know whether it already finished, and what is the exit code, if any.
    /// This can happen if a controller launches a process and then terminates.
    /// When a new controller instance comes online, it may see non-zero ExecutionID Status,
    /// but it does not track the corresponding process or IDE session.
    /// </summary>
    public const string Unknown = "Unknown";
}

internal sealed class Executable : CustomResource<ExecutableSpec, ExecutableStatus>
{
    public const string LaunchConfigurationsAnnotation = "executable.usvc-dev.developer.microsoft.com/launch-configurations";

    [JsonConstructor]
    public Executable(ExecutableSpec spec) : base(spec) { }

    public static Executable Create(string name, string executablePath)
    {
        var exe = new Executable(new ExecutableSpec
        {
            ExecutablePath = executablePath,
        });
        exe.Kind = Dcp.ExecutableKind;
        exe.ApiVersion = Dcp.GroupVersion.ToString();
        exe.Metadata.Name = name;
        exe.Metadata.NamespaceProperty = string.Empty;

        return exe;
    }

    public bool LogsAvailable =>
        this.Status?.State == ExecutableState.Running
        || this.Status?.State == ExecutableState.Finished
        || this.Status?.State == ExecutableState.Terminated;

    public void SetProjectLaunchConfiguration(ProjectLaunchConfiguration launchConfiguration)
    {
        // In Aspire v1 only one launch configuration, of type "project", is supported.
        // Further, there can be only one instance of project launch configuration per Executable.

        this.Annotate(LaunchConfigurationsAnnotation, string.Empty); // Clear existing annotation, if any.
        this.AnnotateAsObjectList(LaunchConfigurationsAnnotation, launchConfiguration);
    }

    public bool TryGetProjectLaunchConfiguration([NotNullWhen(true)] out ProjectLaunchConfiguration? launchConfiguration)
    {
        launchConfiguration = null;
        if (this.TryGetAnnotationAsObjectList(LaunchConfigurationsAnnotation, out List<ProjectLaunchConfiguration>? launchConfigurations))
        {
            // See above regarding how many launch configurations are currently supported.
            launchConfiguration = launchConfigurations?.FirstOrDefault();
        }

        return launchConfiguration is not null;
    }
}

internal static class ProjectLaunchMode
{
    public const string Debug = "Debug";
    public const string NoDebug = "NoDebug";
}

internal sealed class ProjectLaunchConfiguration
{
    [JsonPropertyName("type")]
#pragma warning disable CA1822 // We want this member to be non-static, as it is used in serialization.
    public string Type => "project";
#pragma warning restore CA1822

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = System.Diagnostics.Debugger.IsAttached ? ProjectLaunchMode.Debug : ProjectLaunchMode.NoDebug;

    [JsonPropertyName("project_path")]
    public string ProjectPath { get; set; } = string.Empty;

    [JsonPropertyName("launch_profile")]
    public string LaunchProfile { get; set; } = string.Empty;

    [JsonPropertyName("disable_launch_profile")]
    public bool DisableLaunchProfile { get; set; } = false;
}

