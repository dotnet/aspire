// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Templates.Tests;
using Xunit;

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Extension of ToolCommand for executing CLI scripts safely in tests.
/// </summary>
public class ScriptToolCommand : ToolCommand
{
    private readonly TestEnvironment _env;
    private readonly string _scriptPath;
    private readonly bool _isShellScript;

    public ScriptToolCommand(string scriptPath, TestEnvironment env, ITestOutputHelper testOutput)
        : base(GetExecutable(scriptPath), testOutput)
    {
        _env = env;
        _scriptPath = scriptPath;
        _isShellScript = scriptPath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase);
        
        ConfigureForScript();
    }

    private static string GetExecutable(string scriptPath)
    {
        if (scriptPath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
        {
            return "/bin/bash";
        }
        else if (scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
        {
            return "pwsh";
        }
        throw new ArgumentException($"Unsupported script type: {scriptPath}");
    }

    private void ConfigureForScript()
    {
        // Set environment variables for isolation
        this.WithEnvironmentVariable("HOME", _env.MockHome)
            .WithEnvironmentVariable("USERPROFILE", _env.MockHome)
            .WithEnvironmentVariable("SHELL", "/bin/sh")
            .WithEnvironmentVariable("CI", "true")
            .WithEnvironmentVariable("TERM", "dumb")
            .WithTimeout(TimeSpan.FromSeconds(60));
    }

    protected override string GetFullArgs(params string[] args)
    {
        // Build the full argument string
        var allArgs = new List<string>();
        
        if (_isShellScript)
        {
            // For bash: bash scriptpath arg1 arg2...
            allArgs.Add(_scriptPath);
        }
        else
        {
            // For PowerShell: pwsh -NoProfile -NonInteractive -File scriptpath arg1 arg2...
            allArgs.Add("-NoProfile");
            allArgs.Add("-NonInteractive");
            allArgs.Add("-File");
            allArgs.Add(_scriptPath);
        }
        
        allArgs.AddRange(args);
        return string.Join(" ", allArgs);
    }
}
