// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Exec;

internal class ExecLogger : ILogger, IDisposable
{
    private readonly Channel<(LogLevel, string)> _logChannel;
    private readonly ILogger _serviceLogger;

    public ExecLogger(ILogger serviceLogger, Channel<(LogLevel, string)> logChannel)
    {
        _serviceLogger = serviceLogger;
        _logChannel = logChannel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _serviceLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _serviceLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);

        _logChannel.Writer.TryWrite((logLevel, formatter(state, exception)));
        _serviceLogger.Log(logLevel, eventId, state, exception, formatter);
    }

    public void Complete() => _logChannel.Writer.Complete();

    public void Dispose()
    {
        Complete();
    }
}
