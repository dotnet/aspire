// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class DashboardLifecycleHookTests
{
    [Theory]
    [MemberData(nameof(Data))]
    public async Task WatchDashboardLogs_WrittenToHostLoggerFactory(string logMessage, string expectedMessage, string expectedCategory, LogLevel expectedLevel)
    {
        // Arrange
        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddProvider(new TestLoggerProvider(testSink));
        });
        var logChannel = Channel.CreateUnbounded<WriteContext>();
        testSink.MessageLogged += c => logChannel.Writer.TryWrite(c);

        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = new ResourceNotificationService(NullLogger<ResourceNotificationService>.Instance, new TestHostApplicationLifetime());
        var configuration = new ConfigurationBuilder().Build();
        var hook = new DashboardLifecycleHook(
            configuration,
            Options.Create(new DashboardOptions { DashboardPath = "test.dll" }),
            NullLogger<DistributedApplication>.Instance,
            new TestDashboardEndpointProvider(),
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resourceNotificationService,
            resourceLoggerService,
            factory);

        var model = new DistributedApplicationModel(new ResourceCollection());
        await hook.BeforeStartAsync(model, CancellationToken.None);

        await resourceNotificationService.PublishUpdateAsync(model.Resources.Single(), s => s);

        await foreach (var item in resourceLoggerService.WatchAnySubscribersAsync())
        {
            if (item.Name == KnownResourceNames.AspireDashboard && item.AnySubscribers)
            {
                break;
            }
        }

        // Act
        var dashboardLogger = resourceLoggerService.GetLogger(KnownResourceNames.AspireDashboard);
        dashboardLogger.LogError(logMessage);

        // Assert
        var logContext = await logChannel.Reader.ReadAsync();
        Assert.Equal(expectedCategory, logContext.LoggerName);
        Assert.Equal(expectedMessage, logContext.Message);
        Assert.Equal(expectedLevel, logContext.LogLevel);
    }

    public static IEnumerable<object[]> Data()
    {
        var timestamp = new DateTime(2001, 12, 29, 23, 59, 59, DateTimeKind.Utc);
        var message = new DashboardLogMessage
        {
            LogLevel = LogLevel.Error,
            Category = "TestCategory",
            Message = "Hello world",
            Timestamp = timestamp.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.InvariantCulture),
        };
        var messageJson = JsonSerializer.Serialize(message, DashboardLogMessageContext.Default.DashboardLogMessage);

        yield return new object[]
        {
            $"{DateTime.UtcNow.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.InvariantCulture)} {messageJson}",
            "Hello world",
            "Aspire.Hosting.Dashboard.TestCategory",
            LogLevel.Error
        };
        yield return new object[]
        {
            $"{DateTime.UtcNow.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.InvariantCulture)}{messageJson}",
            "Hello world",
            "Aspire.Hosting.Dashboard.TestCategory",
            LogLevel.Error
        };
        yield return new object[]
        {
            messageJson,
            "Hello world",
            "Aspire.Hosting.Dashboard.TestCategory",
            LogLevel.Error
        };

        message = new DashboardLogMessage
        {
            LogLevel = LogLevel.Critical,
            Category = "TestCategory.TestSubCategory",
            Message = "Error message",
            Exception = new InvalidOperationException("Error!").ToString(),
            Timestamp = timestamp.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.InvariantCulture),
        };
        messageJson = JsonSerializer.Serialize(message, DashboardLogMessageContext.Default.DashboardLogMessage);

        yield return new object[]
        {
            messageJson,
            $"Error message{Environment.NewLine}System.InvalidOperationException: Error!",
            "Aspire.Hosting.Dashboard.TestCategory.TestSubCategory",
            LogLevel.Critical
        };
    }

    private sealed class TestDashboardEndpointProvider : IDashboardEndpointProvider
    {
        public Task<string> GetResourceServiceUriAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted { get; }
        public CancellationToken ApplicationStopped { get; }
        public CancellationToken ApplicationStopping { get; }

        public void StopApplication()
        {
        }
    }
}
