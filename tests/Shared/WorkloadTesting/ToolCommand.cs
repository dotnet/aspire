// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

#nullable enable

namespace Aspire.Workload.Tests;

public class ToolCommand : IDisposable
{
    private readonly string _label;
    private TimeSpan? _timeout;
    protected ITestOutputHelper _testOutput;

    protected string _command;

    public Process? CurrentProcess { get; private set; }

    public Dictionary<string, string> Environment { get; } = new Dictionary<string, string>();

    public event DataReceivedEventHandler? ErrorDataReceived;

    public event DataReceivedEventHandler? OutputDataReceived;

    public string? WorkingDirectory { get; set; }

    public ToolCommand(string command, ITestOutputHelper testOutput, string label="")
    {
        _command = command;
        _testOutput = testOutput;
        _label = label;
    }

    public ToolCommand WithWorkingDirectory(string dir)
    {
        WorkingDirectory = dir;
        return this;
    }

    public ToolCommand WithEnvironmentVariable(string key, string value)
    {
        Environment[key] = value;
        return this;
    }

    public ToolCommand WithEnvironmentVariables(IDictionary<string, string>? extraEnvVars)
    {
        if (extraEnvVars != null)
        {
            foreach ((string key, string value) in extraEnvVars)
            {
                Environment[key] = value;
            }
        }

        return this;
    }

    public ToolCommand WithOutputDataReceived(Action<string?> handler)
    {
        OutputDataReceived += (_, args) => handler(args.Data);
        return this;
    }

    public ToolCommand WithErrorDataReceived(Action<string?> handler)
    {
        ErrorDataReceived += (_, args) => handler(args.Data);
        return this;
    }

    public ToolCommand WithTimeout(TimeSpan timeSpan)
    {
        _timeout = timeSpan;
        return this;
    }

    public virtual async Task<CommandResult> ExecuteAsync(params string[] args)
    {
        var resolvedCommand = _command;
        string fullArgs = GetFullArgs(args);
        _testOutput.WriteLine($"[{_label}] Executing - {resolvedCommand} {fullArgs} {WorkingDirectoryInfo()}");
        CancellationTokenSource cts = new();
        if (_timeout is not null)
        {
            cts.CancelAfter((int)_timeout.Value.TotalMilliseconds);
        }
        return await ExecuteAsyncInternal(resolvedCommand, fullArgs, cts.Token);
    }

    public virtual Task<CommandResult> ExecuteAsync(CancellationToken token, params string[] args)
    {
        var resolvedCommand = _command;
        string fullArgs = GetFullArgs(args);
        _testOutput.WriteLine($"[{_label}] Executing - {resolvedCommand} {fullArgs} {WorkingDirectoryInfo()}");
        return ExecuteAsyncInternal(resolvedCommand, fullArgs, token);
    }

    public virtual void Dispose()
    {
        if (CurrentProcess is not null && !CurrentProcess.HasExited)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CurrentProcess.CloseMainWindow();
            }
            CurrentProcess.Kill(entireProcessTree: true);
            CurrentProcess.Dispose();
            CurrentProcess = null;
        }
    }

    protected virtual string GetFullArgs(params string[] args) => string.Join(" ", args);

    private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args, CancellationToken token)
    {
        var output = new List<string>();
        CurrentProcess = CreateProcess(executable, args);
        // FIXME: lock?
        string msgPrefix = string.IsNullOrEmpty(_label) ? string.Empty : $"[{_label}] ";
        CurrentProcess.ErrorDataReceived += (s, e) =>
        {
            if (e.Data == null)
            {
                return;
            }

            string msg = $"{msgPrefix}{e.Data}";
            output.Add(msg);
            _testOutput.WriteLine(msg);
            ErrorDataReceived?.Invoke(s, e);
        };

        CurrentProcess.OutputDataReceived += (s, e) =>
        {
            if (e.Data == null)
            {
                return;
            }

            string msg = $"{msgPrefix}{e.Data}";
            output.Add(msg);
            _testOutput.WriteLine(msg);
            OutputDataReceived?.Invoke(s, e);
        };

        try
        {
            var exitedTask = CurrentProcess.StartAndWaitForExitAsync();
            CurrentProcess.BeginOutputReadLine();
            CurrentProcess.BeginErrorReadLine();
            await exitedTask.WaitAsync(token);

            RemoveNullTerminator(output);

            return new CommandResult(
                CurrentProcess.StartInfo,
                CurrentProcess.ExitCode,
                string.Join(System.Environment.NewLine, output));
        }
        catch (Exception ex)
        {
            _testOutput.WriteLine($"Exception: {ex}");
            if (!CurrentProcess.HasExited)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    CurrentProcess.CloseMainWindow();
                }

                _testOutput.WriteLine($"Killing");
                CurrentProcess.Kill(entireProcessTree: true);
            }
            CurrentProcess.Dispose();
            throw;
        }
    }

    private Process CreateProcess(string executable, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        psi.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        // runtime repo sets this, which interferes with the tests
        psi.EnvironmentVariables.Remove("MSBuildSDKsPath");

        AddEnvironmentVariablesTo(psi);
        AddWorkingDirectoryTo(psi);
        var process = new Process
        {
            StartInfo = psi
        };

        process.EnableRaisingEvents = true;
        return process;
    }

    private string WorkingDirectoryInfo()
    {
        if (WorkingDirectory == null)
        {
            return "";
        }

        return $" in pwd {WorkingDirectory}";
    }

    private static void RemoveNullTerminator(List<string> strings)
    {
        var count = strings.Count;

        if (count < 1)
        {
            return;
        }

        if (strings[count - 1] == null)
        {
            strings.RemoveAt(count - 1);
        }
    }

    private void AddEnvironmentVariablesTo(ProcessStartInfo psi)
    {
        foreach (var item in Environment)
        {
            _testOutput.WriteLine($"\t[{item.Key}] = {item.Value}");
            psi.Environment[item.Key] = item.Value;
        }
    }

    private void AddWorkingDirectoryTo(ProcessStartInfo psi)
    {
        if (!string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            psi.WorkingDirectory = WorkingDirectory;
        }
    }
}
