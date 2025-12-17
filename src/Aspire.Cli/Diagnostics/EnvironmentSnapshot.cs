// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli.Diagnostics;

/// <summary>
/// Contains diagnostic information about the CLI environment.
/// </summary>
internal sealed class EnvironmentSnapshot
{
    [JsonPropertyName("cli")]
    public required CliInfo Cli { get; init; }
    
    [JsonPropertyName("os")]
    public required OsInfo Os { get; init; }
    
    [JsonPropertyName("dotnet")]
    public required DotNetInfo DotNet { get; init; }
    
    [JsonPropertyName("process")]
    public required ProcessInfo Process { get; init; }
    
    [JsonPropertyName("docker")]
    public required DockerInfo Docker { get; init; }
    
    [JsonPropertyName("environment")]
    public required Dictionary<string, string> Environment { get; init; }
}

/// <summary>
/// CLI version and mode information.
/// </summary>
internal sealed class CliInfo
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    
    [JsonPropertyName("debugMode")]
    public required bool DebugMode { get; init; }
    
    [JsonPropertyName("verboseMode")]
    public required bool VerboseMode { get; init; }
}

/// <summary>
/// Operating system information.
/// </summary>
internal sealed class OsInfo
{
    [JsonPropertyName("platform")]
    public required string Platform { get; init; }
    
    [JsonPropertyName("architecture")]
    public required string Architecture { get; init; }
    
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    
    [JsonPropertyName("is64Bit")]
    public required bool Is64Bit { get; init; }
}

/// <summary>
/// .NET runtime information.
/// </summary>
internal sealed class DotNetInfo
{
    [JsonPropertyName("runtimeVersion")]
    public required string RuntimeVersion { get; init; }
    
    [JsonPropertyName("processArchitecture")]
    public required string ProcessArchitecture { get; init; }
}

/// <summary>
/// CLI process information.
/// </summary>
internal sealed class ProcessInfo
{
    [JsonPropertyName("processId")]
    public required int ProcessId { get; init; }
    
    [JsonPropertyName("workingDirectory")]
    public required string WorkingDirectory { get; init; }
    
    [JsonPropertyName("userName")]
    public required string UserName { get; init; }
    
    [JsonPropertyName("machineName")]
    public required string MachineName { get; init; }
}

/// <summary>
/// Docker availability and version information.
/// </summary>
internal sealed class DockerInfo
{
    [JsonPropertyName("available")]
    public required bool Available { get; init; }
    
    [JsonPropertyName("clientVersion")]
    public string? ClientVersion { get; init; }
    
    [JsonPropertyName("serverVersion")]
    public string? ServerVersion { get; init; }
    
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
