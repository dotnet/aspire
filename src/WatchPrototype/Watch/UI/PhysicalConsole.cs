// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.DotNet.Watch
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal sealed class PhysicalConsole : IConsole
    {
        public const char CtrlC = '\x03';
        public const char CtrlR = '\x12';

        public event Action<ConsoleKeyInfo>? KeyPressed;

        public PhysicalConsole(TestFlags testFlags)
        {
            Console.OutputEncoding = Encoding.UTF8;
            _ = testFlags.HasFlag(TestFlags.ReadKeyFromStdin) ? ListenToStandardInputAsync() : ListenToConsoleKeyPressAsync();
        }

        private async Task ListenToStandardInputAsync()
        {
            using var stream = Console.OpenStandardInput();
            var buffer = new byte[1];

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, CancellationToken.None);
                if (bytesRead != 1)
                {
                    break;
                }

                var c = (char)buffer[0];

                // emulate propagation of Ctrl+C/SIGTERM to child processes
                if (c == CtrlC)
                {
                    Console.WriteLine("Received CTRL+C key");

                    foreach (var processId in ProcessRunner.GetRunningApplicationProcesses())
                    {
                        string? error;
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            Console.WriteLine($"Sending Ctrl+C to {processId}");
                            error = ProcessUtilities.SendWindowsCtrlCEvent(processId);
                        }
                        else
                        {
                            Console.WriteLine($"Sending SIGTERM to {processId}");
                            error = ProcessUtilities.SendPosixSignal(processId, ProcessUtilities.SIGTERM);
                        }

                        if (error != null)
                        {
                            throw new InvalidOperationException(error);
                        }
                    }
                }

                // handle all input keys that watcher might consume:
                var key = c switch
                {
                    CtrlC => new ConsoleKeyInfo('C', ConsoleKey.C, shift: false, alt: false, control: true),
                    CtrlR => new ConsoleKeyInfo('R', ConsoleKey.R, shift: false, alt: false, control: true),
                    >= 'a' and <= 'z' => new ConsoleKeyInfo(c, ConsoleKey.A + (c - 'a'), shift: false, alt: false, control: false),
                    >= 'A' and <= 'Z' => new ConsoleKeyInfo(c, ConsoleKey.A + (c - 'A'), shift: true, alt: false, control: false),
                    _ => default
                };

                if (key.Key != ConsoleKey.None)
                {
                    KeyPressed?.Invoke(key);
                }
            }
        }

        private Task ListenToConsoleKeyPressAsync()
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                KeyPressed?.Invoke(new ConsoleKeyInfo(CtrlC, ConsoleKey.C, shift: false, alt: false, control: true));
            };

            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var key = Console.ReadKey(intercept: true);
                    KeyPressed?.Invoke(key);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public TextWriter Error => Console.Error;
        public TextWriter Out => Console.Out;

        public ConsoleColor ForegroundColor
        {
            get => Console.ForegroundColor;
            set => Console.ForegroundColor = value;
        }

        public void ResetColor() => Console.ResetColor();
        public void Clear() => Console.Clear();
    }
}
