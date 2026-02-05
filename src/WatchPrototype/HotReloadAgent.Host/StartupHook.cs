// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.HotReload;

/// <summary>
/// The runtime startup hook looks for top-level type named "StartupHook".
/// </summary>
internal sealed class StartupHook
{
    private static readonly string? s_standardOutputLogPrefix = Environment.GetEnvironmentVariable(AgentEnvironmentVariables.HotReloadDeltaClientLogMessages);
    private static readonly string? s_namedPipeName = Environment.GetEnvironmentVariable(AgentEnvironmentVariables.DotNetWatchHotReloadNamedPipeName);
    private static readonly bool s_supportsConsoleColor = !OperatingSystem.IsAndroid()
                                                       && !OperatingSystem.IsIOS()
                                                       && !OperatingSystem.IsTvOS()
                                                       && !OperatingSystem.IsBrowser();
    private static readonly bool s_supportsPosixSignals = s_supportsConsoleColor;

#if NET10_0_OR_GREATER
    private static PosixSignalRegistration? s_signalRegistration;
#endif

    /// <summary>
    /// Invoked by the runtime when the containing assembly is listed in DOTNET_STARTUP_HOOKS.
    /// </summary>
    public static void Initialize()
    {
        var processPath = Environment.GetCommandLineArgs().FirstOrDefault();
        var processDir = Path.GetDirectoryName(processPath)!;

        Log($"Loaded into process: {processPath} ({typeof(StartupHook).Assembly.Location})");

        HotReloadAgent.ClearHotReloadEnvironmentVariables(typeof(StartupHook));

        if (string.IsNullOrEmpty(s_namedPipeName))
        {
            Log($"Environment variable {AgentEnvironmentVariables.DotNetWatchHotReloadNamedPipeName} has no value");
            return;
        }

        RegisterSignalHandlers();

        PipeListener? listener = null;

        var agent = new HotReloadAgent(
            assemblyResolvingHandler: (_, args) =>
            {
                Log($"Resolving '{args.Name}, Version={args.Version}'");
                var path = Path.Combine(processDir, args.Name + ".dll");
                return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
            },
            hotReloadExceptionCreateHandler: (code, message) =>
            {
                // Continue executing the code if the debugger is attached.
                // It will throw the exception and the debugger will handle it.
                if (Debugger.IsAttached)
                {
                    return;
                }

                Debug.Assert(listener != null);
                Log($"Runtime rude edit detected: '{message}'");

                SendAndForgetAsync().Wait();

                // Handle Ctrl+C to terminate gracefully:
                Console.CancelKeyPress += (_, _) => Environment.Exit(0);

                // wait for the process to be terminated by the Hot Reload client (other threads might still execute):
                Thread.Sleep(Timeout.Infinite);

                async Task SendAndForgetAsync()
                {
                    try
                    {
                        await listener.SendResponseAsync(new HotReloadExceptionCreatedNotification(code, message), CancellationToken.None);
                    }
                    catch
                    {
                        // do not crash the app
                    }
                }
            });

        listener = new PipeListener(s_namedPipeName, agent, Log);

        // fire and forget:
        _ = listener.Listen(CancellationToken.None);
    }

    private static void RegisterSignalHandlers()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Enables handling of Ctrl+C in a process where it was disabled.
            // 
            // If a process is launched with CREATE_NEW_PROCESS_GROUP flag
            // it allows the parent process to send Ctrl+C event to the child process,
            // but also disables Ctrl+C handlers.
            // 
            // "If the HandlerRoutine parameter is NULL, a TRUE value causes the calling process to ignore CTRL+C input,
            // and a FALSE value restores normal processing of CTRL+C input.
            // This attribute of ignoring or processing CTRL+C is inherited by child processes."

            if (SetConsoleCtrlHandler(null, false))
            {
                Log("Windows Ctrl+C handling enabled.");
            }
            else
            {
                Log($"Failed to enable Ctrl+C handling: {GetLastPInvokeErrorMessage()}");
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool SetConsoleCtrlHandler(Delegate? handler, bool add);
        }
        else if (s_supportsPosixSignals)
        {
#if NET10_0_OR_GREATER
            // Register a handler for SIGTERM to allow graceful shutdown of the application on Unix.
            // See https://github.com/dotnet/docs/issues/46226.

            // Note: registered handlers are executed in reverse order of their registration.
            // Since the startup hook is executed before any code of the application, it is the first handler registered and thus the last to run.

            s_signalRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                Log($"SIGTERM received. Cancel={context.Cancel}");

                if (!context.Cancel)
                {
                    Environment.Exit(0);
                }
            });

            Log("Posix signal handlers registered.");
#endif
        }
    }

    private static string GetLastPInvokeErrorMessage()
    {
        var error = Marshal.GetLastPInvokeError();
#if NET10_0_OR_GREATER
        return $"{Marshal.GetPInvokeErrorMessage(error)} (code {error})";
#else
        return $"error code {error}";
#endif
    }

    private static void Log(string message)
    {
        var prefix = s_standardOutputLogPrefix;
        if (!string.IsNullOrEmpty(prefix))
        {
            if (s_supportsConsoleColor)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            Console.Error.WriteLine($"{prefix} {message}");

            if (s_supportsConsoleColor)
            {
                Console.ResetColor();
            }
        }
    }
}
