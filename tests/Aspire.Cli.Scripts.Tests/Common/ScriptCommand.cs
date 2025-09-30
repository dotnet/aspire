// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Helper class for executing CLI scripts safely in tests.
/// </summary>
public class ScriptCommand
{
    private readonly string _scriptPath;
    private readonly TestEnvironment _env;
    private readonly ITestOutputHelper _output;

    public ScriptCommand(string scriptPath, TestEnvironment env, ITestOutputHelper output)
    {
        _scriptPath = scriptPath;
        _env = env;
        _output = output;
    }

    public Task<ScriptResult> ExecuteAsync(params string[] args)
    {
        string executable;
        List<string> commandArgs = new();

        var isShellScript = _scriptPath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase);
        var isPowerShellScript = _scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);

        if (isShellScript)
        {
            executable = "/bin/bash";
            commandArgs.Add(_scriptPath);
            commandArgs.AddRange(args);
        }
        else if (isPowerShellScript)
        {
            // Use pwsh if available, otherwise powershell
            executable = "pwsh";
            commandArgs.Add("-NoProfile");
            commandArgs.Add("-NonInteractive");
            commandArgs.Add("-File");
            commandArgs.Add(_scriptPath);
            commandArgs.AddRange(args);
        }
        else
        {
            throw new ArgumentException($"Unsupported script type: {_scriptPath}");
        }

        var psi = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in commandArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        // Set environment variables for isolation
        psi.Environment["HOME"] = _env.MockHome;
        psi.Environment["USERPROFILE"] = _env.MockHome;
        psi.Environment["SHELL"] = "/bin/sh";
        psi.Environment["CI"] = "true";
        psi.Environment["TERM"] = "dumb";

        var output = new List<string>();
        using var process = new Process { StartInfo = psi };
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.Add(e.Data);
                _output.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.Add(e.Data);
                _output.WriteLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timeout = TimeSpan.FromSeconds(60);
        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort
            }
            throw new TimeoutException($"Script execution timed out after {timeout.TotalSeconds} seconds");
        }

        return Task.FromResult(new ScriptResult
        {
            ExitCode = process.ExitCode,
            Output = string.Join(Environment.NewLine, output)
        });
    }
}

public class ScriptResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
}
