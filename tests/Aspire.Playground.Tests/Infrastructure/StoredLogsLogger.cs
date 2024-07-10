// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace SamplesIntegrationTests.Infrastructure;

/// <summary>
/// A logger that stores logs in a <see cref="LoggerLogStore"/>.
/// </summary>
/// <param name="logStore"></param>
/// <param name="scopeProvider"></param>
/// <param name="categoryName"></param>
internal sealed class StoredLogsLogger(LoggerLogStore logStore, LoggerExternalScopeProvider scopeProvider, string categoryName) : ILogger
{
    public string CategoryName { get; } = categoryName;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopeProvider.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        logStore.AddLog(CategoryName, logLevel, formatter(state, exception), exception);
    }
}
