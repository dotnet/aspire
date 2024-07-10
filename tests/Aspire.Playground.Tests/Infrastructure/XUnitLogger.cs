// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SamplesIntegrationTests.Infrastructure;

/// <summary>
/// An <see cref="ILogger"/> that writes log messages to an <see cref="ITestOutputHelper"/>.
/// </summary>
internal sealed class XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName) : ILogger
{
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopeProvider.Push(state);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var sb = new StringBuilder();

        sb.Append(DateTime.Now.ToString("O")).Append(' ')
          .Append(GetLogLevelString(logLevel))
          .Append(" [").Append(categoryName).Append("] ")
          .Append(formatter(state, exception));

        if (exception is not null)
        {
            sb.AppendLine().Append(exception);
        }

        // Append scopes
        scopeProvider.ForEachScope((scope, state) =>
        {
            state.AppendLine();
            state.Append(" => ");
            state.Append(scope);
        }, sb);

        testOutputHelper.WriteLine(sb.ToString());
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }
}
