// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents.OpenCode;

internal static class OpenCodePaths
{
    public const string McpConfigFileName = "opencode.jsonc";

    public static string GetGlobalMcpConfigPath(CliExecutionContext executionContext, IEnvironment environment)
    {
        if (GetOverride("OPENCODE_CONFIG") is { } configFile)
        {
            return configFile;
        }

        var configDirectory = GetOverride("OPENCODE_CONFIG_DIR")
            ?? Path.Combine(GetOverride("XDG_CONFIG_HOME")
                ?? Path.Combine(executionContext.HomeDirectory.FullName, ".config"), "opencode");

        return Path.Combine(configDirectory, McpConfigFileName);

        string? GetOverride(string name)
            => environment.GetEnvironmentVariable(name) is { Length: > 0 } value
                ? Path.GetFullPath(value, executionContext.WorkingDirectory.FullName)
                : null;
    }
}
