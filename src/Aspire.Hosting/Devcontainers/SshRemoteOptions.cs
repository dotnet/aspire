// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Devcontainers;

/// <summary>
/// SSH Remote configuration values.
/// </summary>
internal class SshRemoteOptions
{
    /// <summary>
    /// When set to true, the apphost is running in an SSH Remote environment.
    /// </summary>
    /// <remarks>
    /// Detected when both VSCODE_IPC_HOOK_CLI and SSH_CONNECTION environment variables are present.
    /// </remarks>
    public bool IsSshRemote { get; set; }
}

internal class ConfigureSshRemoteOptions(IConfiguration configuration) : IConfigureOptions<SshRemoteOptions>
{
    private const string VscodeIpcHookCliEnvironmentVariable = "VSCODE_IPC_HOOK_CLI";
    private const string SshConnectionEnvironmentVariable = "SSH_CONNECTION";

    public void Configure(SshRemoteOptions options)
    {
        // SSH Remote is detected when both VSCODE_IPC_HOOK_CLI and SSH_CONNECTION environment variables are present
        var hasVscodeIpcHook = !string.IsNullOrEmpty(configuration.GetValue<string>(VscodeIpcHookCliEnvironmentVariable));
        var hasSshConnection = !string.IsNullOrEmpty(configuration.GetValue<string>(SshConnectionEnvironmentVariable));
        
        options.IsSshRemote = hasVscodeIpcHook && hasSshConnection;
    }
}