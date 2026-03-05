// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Intercepts select log messages reported by watch and forwards them to <see cref="WatchStatusWriter"/> to be sent to an external listener.
/// </summary>
internal sealed class StatusReportingLoggerFactory(WatchStatusWriter writer, LoggerFactory underlyingFactory) : ILoggerFactory
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
        => new Logger(writer, underlyingFactory.CreateLogger(categoryName));

    public void AddProvider(ILoggerProvider provider)
        => underlyingFactory.AddProvider(provider);

    private sealed class Logger(WatchStatusWriter writer, ILogger underlyingLogger) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => underlyingLogger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel)
            => logLevel == LogLevel.None || underlyingLogger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            underlyingLogger.Log(logLevel, eventId, state, exception, formatter);

            WatchStatusEvent? status = null;

            if (eventId == MessageDescriptor.BuildStartedNotification.Id)
            {
                var logState = (LogState<IEnumerable<ProjectRepresentation>>)(object)state!;

                status = new WatchStatusEvent
                {
                    Type = WatchStatusEvent.Types.Building,
                    Projects = logState.Arguments.Select(r => r.ProjectOrEntryPointFilePath),
                };
            }
            else if (eventId == MessageDescriptor.BuildCompletedNotification.Id)
            {
                var logState = (LogState<(IEnumerable<ProjectRepresentation> projects, bool success)>)(object)state!;

                status = new WatchStatusEvent
                {
                    Type = WatchStatusEvent.Types.BuildComplete,
                    Projects = logState.Arguments.projects.Select(r => r.ProjectOrEntryPointFilePath),
                    Success = logState.Arguments.success,
                };
            }
            else if (eventId == MessageDescriptor.ChangesAppliedToProjectsNotification.Id)
            {
                var logState = (LogState<IEnumerable<ProjectRepresentation>>)(object)state!;

                status = new WatchStatusEvent
                {
                    Type = WatchStatusEvent.Types.HotReloadApplied,
                    Projects = logState.Arguments.Select(r => r.ProjectOrEntryPointFilePath),
                };
            }
            else if (eventId == MessageDescriptor.RestartingProjectsNotification.Id)
            {
                var logState = (LogState<IEnumerable<ProjectRepresentation>>)(object)state!;

                status = new WatchStatusEvent
                {
                    Type = WatchStatusEvent.Types.Restarting,
                    Projects = logState.Arguments.Select(r => r.ProjectOrEntryPointFilePath)
                };
            }

            if (status != null)
            {
                writer.WriteEvent(status);
            }
        }
    }
}
