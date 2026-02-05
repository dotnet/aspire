// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.Extensions.Logging;

namespace Aspire.Tools.Service;

internal sealed class LoggerProvider(Action<string> reporter) : ILoggerProvider
{
    private sealed class Logger(Action<string> reporter) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (!string.IsNullOrEmpty(message))
            {
                reporter(message);
            }
        }
    }

    public void Dispose()
    {
    }

    public Action<string> Reporter
        => reporter;

    public ILogger CreateLogger(string categoryName)
        => new Logger(reporter);
}
