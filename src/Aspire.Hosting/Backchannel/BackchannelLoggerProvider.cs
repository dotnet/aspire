// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Backchannel;

internal class BackchannelLoggerProvider : ILoggerProvider
{
    private readonly Channel<BackchannelLogEntry> _channel = Channel.CreateUnbounded<BackchannelLogEntry>();
    private readonly IServiceProvider _serviceProvider;
    private readonly object _channelRegisteredLock = new();
    private readonly CancellationTokenSource _backgroundChannelRegistrationCts = new();
    private Task? _backgroundChannelRegistrationTask;

    public BackchannelLoggerProvider(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    private void RegisterLogChannel()
    {
        // Why do we execute this on a background task? This method is spawned on a background
        // task by the CreateLogger method. The CreateLogger method is called when creating many
        // of the services registered in DI - but registering the log channel requires that we
        // can resolve the AppHostRpcTarget service (thus creating a circular dependency). To resolve
        // this we take a dependency on IServiceProvider so that on a separate background task we
        // can resolve AppHostRpcTarget which in turn would have taken a dependency on a logger
        // from this provider.
        var target = _serviceProvider.GetRequiredService<AppHostRpcTarget>();
        target.RegisterLogChannel(_channel);
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (_backgroundChannelRegistrationTask == null)
        {
            lock (_channelRegisteredLock)
            {
                if (_backgroundChannelRegistrationTask == null)
                {
                    _backgroundChannelRegistrationTask = Task.Run(
                        RegisterLogChannel,
                        _backgroundChannelRegistrationCts.Token);
                }
            }
        }

        return new BackchannelLogger(categoryName, _channel);
    }

    public void Dispose()
    {
        _backgroundChannelRegistrationCts.Cancel();
        _channel.Writer.Complete();
    }
}

internal class BackchannelLogger(string categoryName, Channel<BackchannelLogEntry> channel) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return default;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            var entry = new BackchannelLogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                CategoryName = categoryName,
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
            };

            channel.Writer.TryWrite(entry);
        }
    }
}
