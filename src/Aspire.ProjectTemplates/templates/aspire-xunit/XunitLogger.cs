using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Xunit;

public static class XunitLoggerExtensions
{
    public static ILoggingBuilder AddXunit(this ILoggingBuilder logging, ITestOutputHelper testOutputHelper)
    {
        logging.Services.AddXunitLogger(testOutputHelper);
        return logging;
    }

    public static IServiceCollection AddXunitLogger(this IServiceCollection services, ITestOutputHelper testOutputHelper)
    {
        services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(testOutputHelper));
        return services;
    }
}

public class XunitLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    private readonly LoggerExternalScopeProvider _scopeProvider = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(testOutputHelper, _scopeProvider, categoryName);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

internal class XunitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName) : ILogger
{
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopeProvider.Push(state);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var sb = new StringBuilder();

        // Append timestamp, log level, category, and message, e.g: "2021-01-01T00:00:00.0000000Z info [MyCategory] The log message"
        sb.Append(DateTime.Now.ToString("O")).Append(' ')
            .Append(GetLogLevelString(logLevel))
            .Append(" [").Append(categoryName).Append("] ")
            .Append(formatter(state, exception));

        if (exception is not null)
        {
            sb.AppendLine().Append(exception);
        }

        // Append scopes, e.g. " => scope1 => scope2"
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
