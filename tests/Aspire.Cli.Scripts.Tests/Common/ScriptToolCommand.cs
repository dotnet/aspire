// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Templates.Tests;
using Xunit;

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Extends ToolCommand for executing shell and PowerShell scripts.
/// Handles script path resolution and proper argument formatting.
/// </summary>
public sealed class ScriptToolCommand : ToolCommand
{
    private readonly string _scriptPath;
    private readonly bool _isPowerShell;

    /// <summary>
    /// Creates a command for executing a shell or PowerShell script.
    /// </summary>
    /// <param name="scriptPath">Absolute path to the script file.</param>
    /// <param name="testOutput">Test output helper for logging.</param>
    /// <param name="label">Optional label for log messages.</param>
    public ScriptToolCommand(string scriptPath, ITestOutputHelper testOutput, string label = "")
        : base(GetExecutable(scriptPath), testOutput, label)
    {
        _scriptPath = scriptPath;
        _isPowerShell = scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);
        
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Script not found: {scriptPath}");
        }
    }

    private static string GetExecutable(string scriptPath)
    {
        if (scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
        {
            // Use pwsh for PowerShell scripts
            return "pwsh";
        }
        else if (scriptPath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
        {
            // Use bash for shell scripts
            return "bash";
        }
        else
        {
            throw new ArgumentException($"Unsupported script type: {scriptPath}");
        }
    }

    protected override string GetFullArgs(params string[] args)
    {
        if (_isPowerShell)
        {
            // PowerShell: pwsh -File script.ps1 arg1 arg2 ...
            var allArgs = new List<string> { "-File", _scriptPath };
            allArgs.AddRange(args);
            return string.Join(" ", allArgs);
        }
        else
        {
            // Bash: bash script.sh arg1 arg2 ...
            var allArgs = new List<string> { _scriptPath };
            allArgs.AddRange(args);
            return string.Join(" ", allArgs);
        }
    }
}
