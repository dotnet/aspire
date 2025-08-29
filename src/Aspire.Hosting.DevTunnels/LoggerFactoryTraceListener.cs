// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

internal class LoggerFactoryTraceListener(ILoggerFactory loggerFactory) : TraceListener()
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public override bool IsThreadSafe => true;

    public override void Write(string? message) => throw new NotSupportedException();

    public override void WriteLine(string? message) => throw new NotSupportedException();

    public override void WriteLine(string? message, string? category) => throw new NotSupportedException();

    public override void Write(string? message, string? category) => throw new NotSupportedException();

    public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
    {
        if (Filter is not null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
        {
            return;
        }

        var logger = _loggerFactory.CreateLogger(source ?? base.Name);
        var logLevel = ToLogLevel(eventType);
        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, id, "Data logged: {data}", data);
        }
    }

    public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
    {
        if (Filter is not null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
        {
            return;
        }

        var logger = _loggerFactory.CreateLogger(source ?? base.Name);
        var logLevel = ToLogLevel(eventType);
        if (logger.IsEnabled(logLevel))
        {
            var message = "";
            if (data is not null)
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.Append("Data logged: ");
                for (var i = 0; i < data.Length; i++)
                {
                    messageBuilder.Append(CultureInfo.InvariantCulture, $"data{i}");
                    if (i < data.Length)
                    {
                        messageBuilder.Append(", ");
                    }
                }
                message = messageBuilder.ToString();
            }
            logger.Log(logLevel, id, message, data ?? []);
        }
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format, params object?[]? args)
    {
        if (Filter is not null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
        {
            return;
        }

        var logger = _loggerFactory.CreateLogger(source ?? base.Name);
        var logLevel = ToLogLevel(eventType);
        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, id, format, args ?? []);
        }
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
    {
        if (Filter is not null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
        {
            return;
        }

        var logger = _loggerFactory.CreateLogger(source ?? base.Name);
        var logLevel = ToLogLevel(eventType);
        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, id, message);
        }
    }

    public override void Fail(string? message, string? detailMessage)
    {
        var logger = _loggerFactory.CreateLogger(base.Name);
        if (logger.IsEnabled(LogLevel.Error))
        {
            if (detailMessage is not null)
            {
                logger.LogError("{message}: {detailMessage}", message, detailMessage);
            }
            else
            {
                logger.LogError(message);
            }
        }
    }

    private static LogLevel ToLogLevel(TraceEventType eventType) => eventType switch
    {
        TraceEventType.Critical => LogLevel.Critical,
        TraceEventType.Error => LogLevel.Error,
        TraceEventType.Warning => LogLevel.Warning,
        TraceEventType.Information => LogLevel.Information,
        TraceEventType.Verbose => LogLevel.Debug,
        _ => LogLevel.Trace
    };
}