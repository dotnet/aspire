// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Logging;
using Testcontainers.RabbitMq;
using Xunit;

namespace Aspire.RabbitMQ.Client.Tests;

public class AspireRabbitMQLoggingTests
{
    /// <summary>
    /// Tests that the RabbitMQ client logs are forwarded to the M.E.Logging correctly in an end-to-end scenario.
    ///
    /// The easiest way to ensure a log is written is to start the RabbitMQ container, establish the connection,
    /// and then stop the container. This will cause the RabbitMQ client to log an error message.
    /// </summary>
    [Fact]
    [RequiresDocker]
    public async Task EndToEndLoggingTest()
    {
        await using var rabbitMqContainer = new RabbitMqBuilder()
            .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/{RabbitMQContainerImageTags.Image}:{RabbitMQContainerImageTags.Tag}")
            .Build();
        await rabbitMqContainer.StartAsync();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", rabbitMqContainer.GetConnectionString())
        ]);

        builder.AddRabbitMQClient("messaging");

        var tsc = new TaskCompletionSource();
        var logger = new TestLogger();
        logger.LoggedMessage = () =>
        {
            // wait for at least 2 logs to be written
            if (logger.Logs.Count >= 2)
            {
                tsc.SetResult();
            }
        };

        builder.Services.AddSingleton<ILoggerProvider>(sp => new LoggerProvider(logger));

        using var host = builder.Build();
        using var connection = host.Services.GetRequiredService<IConnection>();

        await rabbitMqContainer.StopAsync();
        await rabbitMqContainer.DisposeAsync();

        await tsc.Task.WaitAsync(TimeSpan.FromMinutes(1));

        var logs = logger.Logs.ToArray();
        Assert.True(logs.Length >= 2, "Should be at least 2 logs written.");

        Assert.Contains(logs, l => l.Level == LogLevel.Information && l.Message == "Performing automatic recovery");
        Assert.Contains(logs, l => l.Level == LogLevel.Error && l.Message == "Connection recovery exception.");
    }

    [Fact]
    public void TestInfoAndWarn()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<RabbitMQEventSourceLogForwarder>();

        var logger = new TestLogger();
        builder.Services.AddSingleton<ILoggerProvider>(sp => new LoggerProvider(logger));

        using var host = builder.Build();
        host.Services.GetRequiredService<RabbitMQEventSourceLogForwarder>().Start();

        var message = "This is an informational message.";
        RabbitMqClientEventSource.Log.Info(message);

        var logs = logger.Logs.ToArray();
        Assert.Single(logs);
        Assert.Equal(LogLevel.Information, logs[0].Level);
        Assert.Equal(message, logs[0].Message);

        var warningMessage = "This is a warning message.";
        RabbitMqClientEventSource.Log.Warn(warningMessage);

        logs = logger.Logs.ToArray();
        Assert.Equal(2, logs.Length);
        Assert.Equal(LogLevel.Warning, logs[1].Level);
        Assert.Equal(warningMessage, logs[1].Message);
    }

    [Fact]
    public void TestException()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<RabbitMQEventSourceLogForwarder>();

        var logger = new TestLogger();
        builder.Services.AddSingleton<ILoggerProvider>(sp => new LoggerProvider(logger));

        using var host = builder.Build();
        host.Services.GetRequiredService<RabbitMQEventSourceLogForwarder>().Start();

        var exceptionMessage = "Test exception";
        Exception testException;
        try
        {
            throw new InvalidOperationException(exceptionMessage);
        }
        catch (Exception ex)
        {
            testException = ex;
        }

        Assert.NotNull(testException);
        var logMessage = "This is an error message.";
        RabbitMqClientEventSource.Log.Error(logMessage, testException);

        var logs = logger.Logs.ToArray();
        Assert.Single(logs);
        Assert.Equal(LogLevel.Error, logs[0].Level);
        Assert.Equal(logMessage, logs[0].Message);

        var errorEvent = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(logs[0].State);
        Assert.Equal(5, errorEvent.Count);

        Assert.Equal("message", errorEvent[0].Key);
        Assert.Equal(logMessage, errorEvent[0].Value);

        Assert.Equal("exception.type", errorEvent[1].Key);
        Assert.Equal("System.InvalidOperationException", errorEvent[1].Value);

        Assert.Equal("exception.message", errorEvent[2].Key);
        Assert.Equal(exceptionMessage, errorEvent[2].Value);

        Assert.Equal("exception.stacktrace", errorEvent[3].Key);
        Assert.Contains("AspireRabbitMQLoggingTests.TestException", errorEvent[3].Value?.ToString());

        Assert.Equal("exception.innerexception", errorEvent[4].Key);
        Assert.True(string.IsNullOrEmpty(errorEvent[4].Value?.ToString()));
    }

    private sealed class LoggerProvider(TestLogger logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        public BlockingCollection<(LogLevel Level, string Message, object? State)> Logs { get; } = new();
        public Action? LoggedMessage { get; set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logs.Add((logLevel, formatter(state, exception), state));
            LoggedMessage?.Invoke();
        }
    }
}
