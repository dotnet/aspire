// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

/// <summary>
/// Log an error to dashboard telemetry when there is an unhandled Blazor error.
/// </summary>
public class TelemetryLoggerProvider : ILoggerProvider
{
    private readonly IServiceProvider _serviceProvider;

    public TelemetryLoggerProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILogger CreateLogger(string categoryName) => new TelemetryLogger(_serviceProvider, categoryName);

    public void Dispose()
    {
    }

    private sealed class TelemetryLogger : ILogger
    {
        // Log when an unhandled error is caught by Blazor.
        // https://github.com/dotnet/aspnetcore/blob/0230498dfccaef6f782a5e37c60ea505081b72bf/src/Components/Server/src/Circuits/CircuitHost.cs#L695
        private const string BlazorLogCategory = "Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost";
        private const int CircuitUnhandledExceptionEventId = 111;

        private readonly DashboardTelemetryService? _telemetryService;

        public TelemetryLogger(IServiceProvider serviceProvider, string categoryName)
        {
            if (categoryName == BlazorLogCategory)
            {
                // Set this lazily to avoid a circular reference between resolving telemetry service and logging.
                _telemetryService = serviceProvider.GetRequiredService<DashboardTelemetryService>();
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_telemetryService != null && eventId == CircuitUnhandledExceptionEventId && exception != null)
            {
                _telemetryService.PostFault(
                    TelemetryEventKeys.Error,
                    $"{exception.GetType().FullName}: {exception.Message}",
                    FaultSeverity.Critical,
                    new Dictionary<string, AspireTelemetryProperty>
                    {
                        [TelemetryPropertyKeys.ExceptionType] = new AspireTelemetryProperty(exception.GetType().FullName!),
                        [TelemetryPropertyKeys.ExceptionMessage] = new AspireTelemetryProperty(exception.Message),
                        [TelemetryPropertyKeys.ExceptionStackTrace] = new AspireTelemetryProperty(exception.StackTrace ?? string.Empty)
                    }
                );
            }
        }
    }
}
