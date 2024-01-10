// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Model;

using System.Text.Json.Serialization;
using k8s.Models;

internal sealed class ExecutableSpec
{
    // Path to Executable binary
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }

    // The working directory for the Executable
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    // Launch arguments to be passed to the Executable
    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    // Environment variables to be set for the Executable
    [JsonPropertyName("env")]
    public List<EnvVar>? Env { get; set; }

    // Environment files to use to populate Executable environment during startup.
    [JsonPropertyName("envFiles")]
    public List<string>? EnvFiles { get; set; }

    // The execution type for the Executable
    [JsonPropertyName("executionType")]
    public string? ExecutionType { get; set; }
}

internal static class ExecutionType
{
    // Executable will be run directly by the controller, as a child process
    public const string Process = "Process";

    // Executable will be run via an IDE such as Visual Studio or Visual Studio Code.
    public const string IDE = "IDE";
}

internal sealed class ExecutableStatus : V1Status
{
    // The execution ID is the identifier for the actual-state counterpart of the Executable.
    // For ExecutionType == Process it is the process ID. Process IDs will be eventually reused by OS,
    // but a combination of process ID and startup timestamp is unique for each Executable instance.
    // For ExecutionType == IDE it is the IDE session ID.
    [JsonPropertyName("executionID")]
    public string? ExecutionID { get; set; }

    [JsonPropertyName("pid")]
    public int ProcessId { get; set; }

    // The current state of the process/IDE session started for this executable
    [JsonPropertyName("state")]
    public string? State { get; set; } = ExecutableStates.Unknown;

    // Start (attempt) timestamp.
    [JsonPropertyName("startupTimestamp")]
    public DateTimeOffset? StartupTimestamp { get; set; }

    // The time when the replica finished execution
    [JsonPropertyName("finishTimestamp")]
    public DateTimeOffset? FinishTimestamp { get; set; }

    // Exit code of the process associated with the Executable.
    // The value is equal to UnknownExitCode if the Executable was not started, is still running, or the exit code is not available.
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    // The path of a temporary file that contains captured standard output data from the Executable process.
    [JsonPropertyName("stdOutFile")]
    public string? StdOutFile { get; set; }

    // The path of a temporary file that contains captured standard error data from the Executable process.
    [JsonPropertyName("stdErrFile")]
    public string? StdErrFile { get; set; }

    // Effective values of environment variables, after all substitutions have been applied
    [JsonPropertyName("effectiveEnv")]
    public List<EnvVar>? EffectiveEnv { get; set; }

    // Effective values of launch arguments to be passed to the Executable, after all substitutions are applied.
    [JsonPropertyName("effectiveArgs")]
    public List<string>? EffectiveArgs { get; set; }
}

internal static class ExecutableStates
{
    // Executable was successfully started and was running last time we checked.
    public const string Running = "Running";

    // Terminated means the Executable was killed by the controller (e.g. as a result of scale-down, or object deletion).
    public const string Terminated = "Terminated";

    // Failed to start means the Executable could not be started (e.g. because of invalid path to program file).
    public const string FailedToStart = "FailedToStart";

    // Finished means the Executable ran to completion.
    public const string Finished = "Finished";

    // Unknown means we are not tracking the actual-state counterpart of the Executable (process or IDE run session).
    // As a result, we do not know whether it already finished, and what is the exit code, if any.
    // This can happen if a controller launches a process and then terminates.
    // When a new controller instance comes online, it may see non-zero ExecutionID Status,
    // but it does not track the corresponding process or IDE session.
    public const string Unknown = "Unknown";
}

internal sealed class Executable : CustomResource<ExecutableSpec, ExecutableStatus>
{
    public const string CSharpProjectPathAnnotation = "csharp-project-path";
    public const string CSharpLaunchProfileAnnotation = "csharp-launch-profile";
    public const string OtelServiceNameAnnotation = "otel-service-name";

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
}
