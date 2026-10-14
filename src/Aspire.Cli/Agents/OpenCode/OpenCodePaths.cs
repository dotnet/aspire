// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents.OpenCode;

internal static class OpenCodePaths
{
    public static string GetMcpConfigPath(DirectoryInfo directory)
    {
        var jsoncPath = Path.Combine(directory.FullName, "opencode.jsonc");
        var jsonPath = Path.Combine(directory.FullName, "opencode.json");
        // OpenCode loads JSON first, then JSONC, merging the latter with higher precedence.
        // https://github.com/anomalyco/opencode/blob/dev/packages/core/src/config.ts
        return File.Exists(jsoncPath) || !File.Exists(jsonPath) ? jsoncPath : jsonPath;
    }

    public static string GetConfigDirectory(CliExecutionContext executionContext, IEnvironment environment)
        => Path.Combine(AgentPath.GetOverride("XDG_CONFIG_HOME", executionContext, environment) ??
            Path.Combine(executionContext.HomeDirectory.FullName, ".config"), "opencode");

    public static string GetGlobalMcpConfigPath(CliExecutionContext executionContext, IEnvironment environment)
    {
        if (AgentPath.GetOverride("OPENCODE_CONFIG", executionContext, environment) is { } configFile)
        {
            return configFile;
        }

        var configDirectory = AgentPath.GetOverride("OPENCODE_CONFIG_DIR", executionContext, environment)
            ?? GetConfigDirectory(executionContext, environment);

        return GetMcpConfigPath(new DirectoryInfo(configDirectory));
    }

    public static string GetManagedDirectory(CliExecutionContext executionContext, IEnvironment environment)
    {
        // https://opencode.ai/docs/config/#managed-settings
        if (environment.IsWindows())
        {
            return Path.Combine(AgentPath.GetOverride("ProgramData", executionContext, environment) ??
                Path.Combine(Path.GetPathRoot(executionContext.HomeDirectory.FullName)!, "ProgramData"), "opencode");
        }

        return environment.IsMacOS()
            ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "Library", "Application Support", "opencode")
            : Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", "opencode");
    }
}
