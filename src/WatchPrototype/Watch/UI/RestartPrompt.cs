// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    internal sealed class RestartPrompt(ILogger logger, ConsoleInputReader requester, bool? noPrompt)
    {
        public bool? AutoRestartPreference { get; private set; } = noPrompt;

        public async ValueTask<bool> WaitForRestartConfirmationAsync(string question, CancellationToken cancellationToken)
        {
            if (AutoRestartPreference.HasValue)
            {
                logger.LogInformation("Restarting");
                return AutoRestartPreference.Value;
            }

            var key = await requester.GetKeyAsync(
                $"{question} Yes (y) / No (n) / Always (a) / Never (v)",
                AcceptKey,
                cancellationToken);

            switch (key)
            {
                case ConsoleKey.Escape:
                case ConsoleKey.Y:
                    return true;

                case ConsoleKey.N:
                    return false;

                case ConsoleKey.A:
                    AutoRestartPreference = true;
                    return true;

                case ConsoleKey.V:
                    AutoRestartPreference = false;
                    return false;
            }

            throw new InvalidOperationException();

            static bool AcceptKey(ConsoleKeyInfo info)
                => info is { Key: ConsoleKey.Y or ConsoleKey.N or ConsoleKey.A or ConsoleKey.V, Modifiers: ConsoleModifiers.None };
        }
    }
}
