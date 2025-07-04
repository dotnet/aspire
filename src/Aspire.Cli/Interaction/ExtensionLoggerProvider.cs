// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Interaction;

internal class ExtensionLoggerProvider(IServiceProvider serviceProvider) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ExtensionLogger(serviceProvider);
    }

    public void Dispose()
    {
    }
}

internal class ExtensionLogger(IServiceProvider serviceProvider) : ILogger
{
    private ExtensionInteractionService InteractionService => (ExtensionInteractionService)serviceProvider.GetRequiredService<IInteractionService>();

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        InteractionService.LogMessage(logLevel, formatter(state, exception));
    }
}
