// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Process;

internal sealed class ProcessSpec
{
    public string ExecutablePath { get; }
    public string? WorkingDirectory { get; init; }
    public IDictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();
    public string? Arguments { get; init; }
    public Action<string>? OnOutputData { get; init; }
    public Action<string>? OnErrorData { get; init; }
    public Action<int>? OnStart { get; init; }
    public Action<int>? OnStop { get; init; }
    public bool KillEntireProcessTree { get; init; } = true;
    public bool ThrowOnNonZeroReturnCode { get; init; } = true;

    public ProcessSpec(string executablePath)
    {
        ExecutablePath = executablePath;
    }
}
