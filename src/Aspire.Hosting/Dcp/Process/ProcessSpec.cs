// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Process;

internal sealed class ProcessSpec
{
    public string ExecutablePath { get; set; }
    public string? WorkingDirectory { get; set; }
    public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    public string? Arguments { get; set; }
    public Action<string>? OnOutputData { get; set; }
    public Action<string>? OnErrorData { get; set; }
    public Action<int>? OnStart { get; set; }
    public Action<int>? OnStop { get; set; }
    public bool KillEntireProcessTree { get; set; } = true;
    public bool ThrowOnNonZeroReturnCode { get; set; } = true;

    public ProcessSpec(string executablePath)
    {
        ExecutablePath = executablePath;
    }
}
