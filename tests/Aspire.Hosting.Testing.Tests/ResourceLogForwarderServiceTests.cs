// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Projects;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Testing.Tests;

public class ResourceLogForwarderServiceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task BackgroundServiceIsRegisteredInServiceProvider()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<TestingAppHost1_AppHost>();
        Assert.Contains(appHost.Services, sd =>
            sd.ServiceType == typeof(IHostedService)
            && sd.ImplementationType == typeof(ResourceLoggerForwarderService)
            && sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public async Task ExecuteThowsOperationCanceledWhenAppStoppingTokenSignalled()
    {
        var hostApplicationLifetime = new TestHostApplicationLifetime();
        var resourceNotificationService = new ResourceNotificationService(NullLogger<ResourceNotificationService>.Instance, hostApplicationLifetime);
        var resourceLoggerService = new ResourceLoggerService();
        var hostEnvironment = new HostingEnvironment();
        var loggerFactory = new NullLoggerFactory();
        var resourceLogForwarder = new ResourceLoggerForwarderService(resourceNotificationService, resourceLoggerService, hostEnvironment, loggerFactory);

        await resourceLogForwarder.StartAsync(hostApplicationLifetime.ApplicationStopping);

        Assert.NotNull(resourceLogForwarder.ExecuteTask);
        Assert.Equal(TaskStatus.WaitingForActivation, resourceLogForwarder.ExecuteTask.Status);

        // Signal the stopping token
        hostApplicationLifetime.StopApplication();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await resourceLogForwarder.ExecuteTask;
        });
    }

    [Fact]
    public async Task ResourceLogsAreForwardedToHostLogging()
    {
        var hostApplicationLifetime = new TestHostApplicationLifetime();
        var resourceNotificationService = new ResourceNotificationService(NullLogger<ResourceNotificationService>.Instance, hostApplicationLifetime);
        var resourceLoggerService = new ResourceLoggerService();
        var hostEnvironment = new HostingEnvironment { ApplicationName = "TestApp.AppHost" };
        var fakeLoggerProvider = new FakeLoggerProvider();
        var fakeLoggerFactory = new LoggerFactory([fakeLoggerProvider, new XunitLoggerProvider(output)]);
        var resourceLogForwarder = new ResourceLoggerForwarderService(resourceNotificationService, resourceLoggerService, hostEnvironment, fakeLoggerFactory);

        var logStreamCompleteTcs = new TaskCompletionSource();
        resourceLogForwarder.OnLogStreamComplete = resourceId =>
        {
            if (resourceId == "myresource")
            {
                logStreamCompleteTcs.SetResult();
            }
        };

        await resourceLogForwarder.StartAsync(hostApplicationLifetime.ApplicationStopping);

        // Publish an update to the resource to kickstart the notification service loop
        var myresource = new CustomResource("myresource");
        await resourceNotificationService.PublishUpdateAsync(myresource, snapshot => snapshot with { State = "Running" });

        // Log messages to the resource
        var resourceLogger = resourceLoggerService.GetLogger(myresource);
        fakeLoggerProvider.Collector.Clear();

        resourceLogger.LogTrace("Test trace message");
        resourceLogger.LogDebug("Test debug message");
        resourceLogger.LogInformation("Test information message");
        resourceLogger.LogWarning("Test warning message");
        resourceLogger.LogError("Test error message");
        resourceLogger.LogCritical("Test critical message");

        // Complete the resource log stream and wait for it to end
        resourceLoggerService.Complete(myresource);
        await logStreamCompleteTcs.Task;
        hostApplicationLifetime.StopApplication();

        // Get the logs from the fake logger
        var hostLogs = fakeLoggerProvider.Collector.GetSnapshot();

        // Line number is pre-pended to message
        // Category is derived from the application name and resource name
        // Logs sent at information level or lower are logged as information, otherwise they are logged as error
        Assert.Collection(hostLogs,
            log => { Assert.Equal(LogLevel.Information, log.Level); Assert.Equal("1: Test trace message", log.Message); Assert.Equal("TestApp.AppHost.Resources.myresource", log.Category); },
            log => { Assert.Equal(LogLevel.Information, log.Level); Assert.Equal("2: Test debug message", log.Message); Assert.Equal("TestApp.AppHost.Resources.myresource", log.Category); },
            log => { Assert.Equal(LogLevel.Information, log.Level); Assert.Equal("3: Test information message", log.Message); Assert.Equal("TestApp.AppHost.Resources.myresource", log.Category); },
            log => { Assert.Equal(LogLevel.Information, log.Level); Assert.Equal("4: Test warning message", log.Message); Assert.Equal("TestApp.AppHost.Resources.myresource", log.Category); },
            log => { Assert.Equal(LogLevel.Error, log.Level); Assert.Equal("5: Test error message", log.Message); Assert.Equal("TestApp.AppHost.Resources.myresource", log.Category); },
            log => { Assert.Equal(LogLevel.Error, log.Level); Assert.Equal("6: Test critical message", log.Message); Assert.Equal("TestApp.AppHost.Resources.myresource", log.Category); });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    public async Task FindTheRaceConditionInResourceNotifications(int logLineCountTarget)
    {
        for (var i = 0; i < 10_000; i++)
        {
            var hostApplicationLifetime = new TestHostApplicationLifetime();
            var resourceLoggerService = new ResourceLoggerService();
            var hostEnvironment = new HostingEnvironment { ApplicationName = "TestApp.AppHost" };

            var myresource = new CustomResource("myresource");

            // Start the log subscriber loop
            var loggedLines = new List<string>();
            var logLoop = Task.Run(async () =>
            {
                await foreach (var logLines in resourceLoggerService.WatchAsync(myresource).WithCancellation(hostApplicationLifetime.ApplicationStopping))
                {
                    loggedLines.AddRange(logLines.Select(l => $"{l.LineNumber} {l.Content}"));
                    if (loggedLines.Count >= logLineCountTarget)
                    {
                        break;
                    }
                }
            });

            Assert.Empty(loggedLines);

            // Log messages to the resource
            var resourceLogger = resourceLoggerService.GetLogger(myresource);
            for (var j = 0; j < logLineCountTarget; j++)
            {
                resourceLogger.LogInformation("log message (ticks: {Ticks})", DateTime.Now.Ticks);
            }

            // Wait for the log loop to complete or timeout after 10 seconds
            await Task.WhenAny(logLoop, Task.Delay(10_000));

            // Complete the resource log stream
            resourceLoggerService.Complete(myresource);

            // Trigger the stopping token
            hostApplicationLifetime.StopApplication();

            Assert.True(logLineCountTarget == loggedLines.Count, $"On iteration {i}, expected {logLineCountTarget} log lines but got {loggedLines.Count}:\r\n{string.Join("\r\n", loggedLines)}");
        }
    }

    private sealed class CustomResource(string name) : Resource(name)
    {

    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _startedCts = new();
        private readonly CancellationTokenSource _stoppingCts = new();
        private readonly CancellationTokenSource _stoppedCts = new();

        public TestHostApplicationLifetime(bool startStarted = true)
        {
            ApplicationStarted = _startedCts.Token;
            ApplicationStopping = _stoppingCts.Token;
            ApplicationStopped = _stoppedCts.Token;

            if (startStarted)
            {
                _startedCts.Cancel();
            }
        }

        public CancellationToken ApplicationStarted { get; }
        public CancellationToken ApplicationStopped { get; }
        public CancellationToken ApplicationStopping { get; }

        public void StartApplication()
        {
            _startedCts.Cancel();
        }

        public void StopApplication()
        {
            _stoppingCts.Cancel();
            _stoppedCts.Cancel();
        }

        public void Dispose()
        {
            _stoppingCts.Dispose();
            _stoppedCts.Dispose();
        }
    }
}
