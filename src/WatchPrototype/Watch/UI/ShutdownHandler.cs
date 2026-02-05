// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class ShutdownHandler : IDisposable
{
    private readonly CancellationTokenSource _cancellationSource = new();
    public CancellationToken CancellationToken { get; }

    private volatile bool _disposed;

    public ShutdownHandler(IConsole console, ILogger logger)
    {
        CancellationToken = _cancellationSource.Token;

        console.KeyPressed += key =>
        {
            if (!_disposed && key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.C)
            {
                // if we already canceled, we force immediate shutdown:
                var forceShutdown = _cancellationSource.IsCancellationRequested;

                if (!forceShutdown)
                {
                    logger.Log(MessageDescriptor.ShutdownRequested);
                    _cancellationSource.Cancel();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        };
    }

    public void Dispose()
    {
        _disposed = true;
        _cancellationSource.Dispose();
    }
}
