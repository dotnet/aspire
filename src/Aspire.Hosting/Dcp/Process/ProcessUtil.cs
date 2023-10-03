// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Aspire.Hosting.Dcp.Process;

internal static partial class ProcessUtil
{
    #region Native Methods

    [LibraryImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static partial int sys_kill(int pid, int sig);

    #endregion

    private static readonly TimeSpan s_processExitTimeout = TimeSpan.FromSeconds(5);

    public static (Task<ProcessResult>, IAsyncDisposable) Run(ProcessSpec processSpec)
    {
        var process = new System.Diagnostics.Process()
        {
            StartInfo =
            {
                FileName = processSpec.ExecutablePath,
                WorkingDirectory = processSpec.WorkingDirectory ?? string.Empty,
                Arguments = processSpec.Arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            },
            EnableRaisingEvents = true
        };

        processSpec.EnvironmentVariables.ToList().ForEach(x => process.StartInfo.Environment[x.Key] = x.Value);

        var processEventLock = new object();

        // Note: even though the child process has exited, its children may be alive and still producing output.
        // See https://github.com/dotnet/runtime/issues/29232#issuecomment-1451584094 for how this might affect waiting for process exit.
        // We are going to discard that (grandchild) output by checking process.HasExited.

        if (processSpec.OnOutputData != null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                lock (processEventLock)
                {
                    if (e.Data == null || process.HasExited)
                    {
                        return;
                    }
                }

                processSpec.OnOutputData.Invoke(e.Data); // not holding the event lock
            };
        }

        if (processSpec.OnErrorData != null)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                lock (processEventLock)
                {
                    if (e.Data == null || process.HasExited)
                    {
                        return;
                    }
                }

                processSpec.OnErrorData.Invoke(e.Data); // not holding the event lock
            };
        }

        var processLifetimeTcs = new TaskCompletionSource<ProcessResult>();

        process.Exited += (_, e) =>
        {
            lock (processEventLock)
            {
                if (processSpec.ThrowOnNonZeroReturnCode && process.ExitCode != 0)
                {
                    processLifetimeTcs.TrySetException(new InvalidOperationException(
                        $"Command {processSpec.ExecutablePath} {processSpec.Arguments} returned non-zero exit code {process.ExitCode}"));
                }
                else
                {
                    processLifetimeTcs.TrySetResult(new ProcessResult(process.ExitCode));
                }
            }
        };

        try
        {
            AspireEventSource.Instance.ProcessLaunchStart(processSpec.ExecutablePath, processSpec.Arguments ?? "");

            // Take the event lock to ensure that OnStart() is called before the lifetime task ends.
            lock (processEventLock)
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                processSpec.OnStart?.Invoke(process.Id);
            }
        }
        finally
        {
            AspireEventSource.Instance.ProcessLaunchStop(processSpec.ExecutablePath, processSpec.Arguments ?? "");
        }
        

        return (processLifetimeTcs.Task, new ProcessDisposable(process, processLifetimeTcs.Task, processSpec.KillEntireProcessTree));
    }

    private sealed class ProcessDisposable : IAsyncDisposable
    {
        private readonly System.Diagnostics.Process _process;
        private readonly Task _processLifetimeTask;
        private readonly bool _entireProcessTree;

        public ProcessDisposable(System.Diagnostics.Process process, Task processLifetimeTask, bool entireProcessTree)
        {
            _process = process;
            _processLifetimeTask = processLifetimeTask;
            _entireProcessTree = entireProcessTree;
        }

        public async ValueTask DisposeAsync()
        {
            if (_process.HasExited)
            {
                return; // nothing to do
            }

            if (OperatingSystem.IsWindows())
            {
                if (!_process.CloseMainWindow())
                {
                    _process.Kill(_entireProcessTree);
                }
            }
            else
            {
                sys_kill(_process.Id, sig: 2); // SIGINT
            }

            await _processLifetimeTask.WaitAsync(s_processExitTimeout).ConfigureAwait(false);
            if (!_process.HasExited)
            {
                // Always try to kill the entire process tree here if all of the above has failed.
                _process.Kill(entireProcessTree: true);
            }
        }
    }
}
