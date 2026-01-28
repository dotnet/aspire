// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal sealed class ConsoleReporter(IConsole console, bool suppressEmojis) : IReporter, IProcessOutputReporter
    {
        public bool SuppressEmojis { get; } = suppressEmojis;

        private readonly Lock _writeLock = new();

        bool IProcessOutputReporter.PrefixProcessOutput
            => false;

        void IProcessOutputReporter.ReportOutput(OutputLine line)
        {
            lock (_writeLock)
            {
                (line.IsError ? console.Error : console.Out).WriteLine(line.Content);
            }
        }

        private void WriteLine(TextWriter writer, string message, ConsoleColor? color, Emoji emoji)
        {
            lock (_writeLock)
            {
                console.ForegroundColor = ConsoleColor.DarkGray;
                writer.Write((SuppressEmojis ? Emoji.Default : emoji).GetLogMessagePrefix());
                console.ResetColor();

                if (color.HasValue)
                {
                    console.ForegroundColor = color.Value;
                }

                writer.WriteLine(message);

                if (color.HasValue)
                {
                    console.ResetColor();
                }
            }
        }

        public void Report(EventId id, Emoji emoji, LogLevel level, string message)
        {
            var color = level switch
            {
                LogLevel.Critical or LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Information => (ConsoleColor?)null,
                _ => ConsoleColor.DarkGray,
            };

            // Use stdout for error messages to preserve ordering with respect to other output.
            WriteLine(console.Error, message, color, emoji);
        }
    }
}
