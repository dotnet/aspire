// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Executes CLI scripts in isolated environments with safety guarantees.
/// </summary>
public class ScriptExecutor
{
    /// <summary>
    /// Executes a script with the given arguments in an isolated environment.
    /// </summary>
    /// <param name="scriptPath">Path to the script file (.sh or .ps1)</param>
    /// <param name="env">Test environment for isolated execution</param>
    /// <param name="args">Script arguments</param>
    /// <returns>Script execution result</returns>
    public static async Task<ScriptResult> ExecuteAsync(string scriptPath, TestEnvironment env, params string[] args)
    {
        var isShellScript = scriptPath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase);
        var isPowerShellScript = scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);

        if (!isShellScript && !isPowerShellScript)
        {
            throw new ArgumentException($"Unsupported script type: {scriptPath}");
        }

        // Determine the shell/interpreter to use
        string executable;
        List<string> commandArgs = new();

        if (isShellScript)
        {
            if (OperatingSystem.IsWindows())
            {
                // On Windows, we need bash (from Git Bash, WSL, or similar)
                // For safety, we'll skip shell script tests on Windows if bash is not available
                var bashPath = FindBashOnWindows();
                if (bashPath == null)
                {
                    return new ScriptResult
                    {
                        ExitCode = -1,
                        Output = "",
                        ErrorOutput = "Bash not found on Windows. Shell scripts cannot be tested.",
                        WasSkipped = true
                    };
                }
                executable = bashPath;
            }
            else
            {
                executable = "/bin/bash";
            }
            commandArgs.Add(scriptPath);
        }
        else // PowerShell script
        {
            // Use pwsh if available, otherwise powershell on Windows
            if (IsPwshAvailable())
            {
                executable = "pwsh";
            }
            else if (OperatingSystem.IsWindows())
            {
                executable = "powershell";
            }
            else
            {
                return new ScriptResult
                {
                    ExitCode = -1,
                    Output = "",
                    ErrorOutput = "PowerShell not found on non-Windows platform.",
                    WasSkipped = true
                };
            }

            commandArgs.Add("-NoProfile");
            commandArgs.Add("-NonInteractive");
            commandArgs.Add("-File");
            commandArgs.Add(scriptPath);
        }

        // Add script arguments
        commandArgs.AddRange(args);

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in commandArgs)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // SAFETY: Set mock home directories to prevent real user directory modifications
        if (OperatingSystem.IsWindows())
        {
            var homeDrive = Path.GetPathRoot(env.MockHome)?.TrimEnd(Path.DirectorySeparatorChar) ?? "C:";
            startInfo.Environment["USERPROFILE"] = env.MockHome;
            startInfo.Environment["HOMEDRIVE"] = homeDrive;
            startInfo.Environment["HOMEPATH"] = env.MockHome.Substring(homeDrive.Length);
        }
        else
        {
            startInfo.Environment["HOME"] = env.MockHome;
        }

        // Disable any potential interactive prompts
        startInfo.Environment["CI"] = "true";
        startInfo.Environment["TERM"] = "dumb";
        
        // Set SHELL to indicate no known shell config (prevents shell profile modification attempts)
        startInfo.Environment["SHELL"] = "/bin/sh";

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process { StartInfo = startInfo };
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for process to complete with a timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
        var processTask = Task.Run(process.WaitForExit);
        
        var completedTask = await Task.WhenAny(processTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            // Timeout occurred
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort
            }

            return new ScriptResult
            {
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                ErrorOutput = errorBuilder.ToString() + "\nProcess timed out after 60 seconds.",
                TimedOut = true
            };
        }

        return new ScriptResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            ErrorOutput = errorBuilder.ToString()
        };
    }

    private static string? FindBashOnWindows()
    {
        // Try common locations for bash on Windows
        var possiblePaths = new[]
        {
            @"C:\Program Files\Git\bin\bash.exe",
            @"C:\Program Files (x86)\Git\bin\bash.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Try to find in PATH
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "bash",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output.Split('\n')[0].Trim();
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    private static bool IsPwshAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }
        catch
        {
            // pwsh not available
        }

        return false;
    }
}

/// <summary>
/// Result of script execution.
/// </summary>
public class ScriptResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public bool TimedOut { get; set; }
    public bool WasSkipped { get; set; }

    public bool IsSuccess => ExitCode == 0 && !TimedOut && !WasSkipped;
    public bool IsFailure => ExitCode != 0 || TimedOut;
}
