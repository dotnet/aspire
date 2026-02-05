// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal sealed class ConsoleInputReader(IConsole console, LogLevel logLevel, bool suppressEmojis)
    {
        private readonly object _writeLock = new();

        public async Task<ConsoleKey> GetKeyAsync(string prompt, Func<ConsoleKeyInfo, bool> validateInput, CancellationToken cancellationToken)
        {
            if (logLevel > LogLevel.Information)
            {
                return ConsoleKey.Escape;
            }

            var questionMark = suppressEmojis ? "?" : "❔";
            while (true)
            {
                // Start listening to the key before printing the prompt to avoid race condition
                // in tests that wait for the prompt before sending the key press.
                var tcs = new TaskCompletionSource<ConsoleKey>(TaskCreationOptions.RunContinuationsAsynchronously);
                console.KeyPressed += KeyPressed;

                WriteLine($"  {questionMark} {prompt}");

                lock (_writeLock)
                {
                    console.ForegroundColor = ConsoleColor.DarkGray;
                    console.Out.Write($"  {questionMark} ");
                    console.ResetColor();
                }

                try
                {
                    return await tcs.Task.WaitAsync(cancellationToken);
                }
                catch (ArgumentException)
                {
                    // Prompt again for valid input
                }
                finally
                {
                    console.KeyPressed -= KeyPressed;
                }

                void KeyPressed(ConsoleKeyInfo key)
                {
                    var keyDisplay = GetDisplayString(key);
                    if (validateInput(key))
                    {
                        WriteLine(keyDisplay);
                        tcs.TrySetResult(key.Key);
                    }
                    else
                    {
                        WriteLine(keyDisplay, ConsoleColor.DarkRed);
                        tcs.TrySetException(new ArgumentException($"Invalid key '{keyDisplay}' entered."));
                    }
                }

                static string GetDisplayString(ConsoleKeyInfo key)
                {
                    var keyDisplay = (key.Modifiers == ConsoleModifiers.None) ? key.KeyChar.ToString() : key.Key.ToString();

                    if (key.Modifiers.HasFlag(ConsoleModifiers.Alt))
                    {
                        keyDisplay = "Alt+" + keyDisplay;
                    }

                    if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                    {
                        keyDisplay = "Shift+" + keyDisplay;
                    }

                    if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        keyDisplay = "Ctrl+" + keyDisplay;
                    }

                    return keyDisplay;
                }
            }

            void WriteLine(string message, ConsoleColor color = ConsoleColor.DarkGray)
            {
                lock (_writeLock)
                {
                    console.ForegroundColor = color;
                    console.Out.WriteLine(message);
                    console.ResetColor();
                }
            }
        }
    }
}
