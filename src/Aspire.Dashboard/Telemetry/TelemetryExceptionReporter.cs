// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public class TelemetryExceptionReporter(IServiceProvider serviceProvider) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (exception is not null)
        {
            Task.Run(async () =>
            {
                var serviceScope = serviceProvider.CreateAsyncScope();
                var telemetryService = serviceScope.ServiceProvider.GetRequiredService<DashboardTelemetryService>();
                await telemetryService.InitializeAsync().ConfigureAwait(false);

                telemetryService.PostFault(
                    TelemetryEventKeys.Fault,
                    formatter(state, exception),
                    ToFaultSeverity(logLevel),
                    new Dictionary<string, AspireTelemetryProperty>
                    {
                        {
                            TelemetryPropertyKeys.ExceptionType, new AspireTelemetryProperty(exception.GetType().Name)
                        },
                        { TelemetryPropertyKeys.ExceptionMessage, new AspireTelemetryProperty(exception.Message) },
                        {
                            TelemetryPropertyKeys.ExceptionStackTrace,
                            new AspireTelemetryProperty(exception.StackTrace ?? string.Empty)
                        }
                    }
                );

                await serviceScope.DisposeAsync().ConfigureAwait(false);
            });

        }
    }

    private static FaultSeverity ToFaultSeverity(LogLevel level)
    {
        return level switch
        {
            LogLevel.Critical => FaultSeverity.Critical,
            LogLevel.Error => FaultSeverity.General,
            LogLevel.Warning => FaultSeverity.General,
            _ => FaultSeverity.Diagnostic
        };
    }
}

public class TelemetryExceptionReporterProvider(IServiceProvider serviceProvider) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new TelemetryExceptionReporter(serviceProvider);
    }

    public void Dispose()
    {
    }
}
