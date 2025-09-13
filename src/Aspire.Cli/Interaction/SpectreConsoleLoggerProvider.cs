// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Aspire.Cli.Interaction;

internal class SpectreConsoleLoggerProvider(IServiceProvider serviceProvider) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new SpectreConsoleLogger(serviceProvider, categoryName);
    }

    public void Dispose()
    {
    }
}

internal class SpectreConsoleLogger(IServiceProvider serviceProvider, string categoryName) : ILogger
{
    private IInteractionService InteractionService => serviceProvider.GetRequiredService<IInteractionService>();

    public bool IsEnabled(LogLevel logLevel) => 
        logLevel >= LogLevel.Debug && 
        (categoryName.StartsWith("Aspire.Cli", StringComparison.Ordinal) || logLevel >= LogLevel.Warning);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception is null)
        {
            return;
        }

        var formattedMessage = exception is not null ? $"{message} {exception}" : message;
        
        // Extract the last token from the category name to reduce noise
        var shortCategoryName = GetShortCategoryName(categoryName);
        
        // Format timestamp to show only time (HH:mm:ss) for debugging purposes
        var timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        
        // Use DisplaySubtleMessage for clean debug output
        InteractionService.DisplaySubtleMessage($"[{timestamp}] [{GetLogLevelString(logLevel)}] {shortCategoryName}: {formattedMessage}");
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info", 
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => logLevel.ToString().ToLower()
    };

    private static string GetShortCategoryName(string categoryName)
    {
        var lastDotIndex = categoryName.LastIndexOf('.');
        return lastDotIndex >= 0 ? categoryName.Substring(lastDotIndex + 1) : categoryName;
    }
}