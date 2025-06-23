// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Backchannel;

internal class BackchannelLoggerProvider : ILoggerProvider
{
    private readonly Channel<BackchannelLogEntry> _channel = Channel.CreateUnbounded<BackchannelLogEntry>();
    private readonly DistributedApplicationExecutionContext _context;
    private readonly object _channelRegisteredLock = new();
    private readonly CancellationTokenSource _backgroundChannelRegistrationCts = new();
    private Task? _backgroundChannelRegistrationTask;

    public BackchannelLoggerProvider(DistributedApplicationExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    private async Task RegisterLogChannelAsync(CancellationToken cancellationToken)
    {
        using var periodic = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await periodic.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_context.UnsafeServiceProvider is { } serviceProvider)
            {
                var rpcTarget = serviceProvider.GetRequiredService<AppHostRpcTarget>();
                rpcTarget.RegisterLogChannel(_channel);
                // Resolve backchannel and register log channel so logs can be sent to the backchannel.
            }
        }
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
                        () => RegisterLogChannelAsync(_backgroundChannelRegistrationCts.Token),
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
                Exception = exception,
            };

            channel.Writer.TryWrite(entry);
        }
    }
}

internal class BackchannelLogEntry
{
    public required EventId EventId { get; set; }
    public required LogLevel LogLevel { get; set; }
    public required string Message { get; set; }
    public Exception? Exception { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string CategoryName { get; set; }
}