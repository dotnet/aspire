// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Tests.Utils.Grpc;
using Aspire.Hosting.Utils;
using Aspire.ResourceService.Proto.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using DashboardService = Aspire.Hosting.Dashboard.DashboardService;
using Resource = Aspire.Hosting.ApplicationModel.Resource;

namespace Aspire.Hosting.Tests.Dashboard;

public class DashboardServiceTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task WatchResourceConsoleLogs_LargePendingData_BatchResults()
    {
        // Arrange
        const int LongLineCharacters = DashboardService.LogMaxBatchCharacters / 3;
        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = new ResourceNotificationService(NullLogger<ResourceNotificationService>.Instance, new TestHostApplicationLifetime(), new ServiceCollection().BuildServiceProvider(), resourceLoggerService);
        var dashboardServiceData = new DashboardServiceData(resourceNotificationService, resourceLoggerService, NullLogger<DashboardServiceData>.Instance, new DashboardCommandExecutor(new ServiceCollection().BuildServiceProvider()));
        var dashboardService = new DashboardService(dashboardServiceData, new TestHostEnvironment(), new TestHostApplicationLifetime(), NullLogger<DashboardService>.Instance);

        var logger = resourceLoggerService.GetLogger("test-resource");

        // Exceed limit line
        logger.LogInformation(new string('1', DashboardService.LogMaxBatchCharacters));
        // Three long lines
        logger.LogInformation(new string('2', LongLineCharacters));
        logger.LogInformation(new string('3', LongLineCharacters));
        logger.LogInformation(new string('4', LongLineCharacters));

        var context = TestServerCallContext.Create();
        var writer = new TestServerStreamWriter<WatchResourceConsoleLogsUpdate>(context);

        // Act
        var task = dashboardService.WatchResourceConsoleLogs(
            new WatchResourceConsoleLogsRequest { ResourceName = "test-resource" },
            writer,
            context);

        // Assert
        var exceedLimitUpdate = await writer.ReadNextAsync().DefaultTimeout();
        Assert.Collection(exceedLimitUpdate.LogLines,
            l => Assert.Equal(DashboardService.LogMaxBatchCharacters, l.Text.Length));

        var longLinesUpdate1 = await writer.ReadNextAsync().DefaultTimeout();
        Assert.Collection(longLinesUpdate1.LogLines,
            l => Assert.Equal(LongLineCharacters, l.Text.Split(' ')[1].Length),
            l => Assert.Equal(LongLineCharacters, l.Text.Split(' ')[1].Length));

        var longLinesUpdate2 = await writer.ReadNextAsync().DefaultTimeout();
        Assert.Collection(longLinesUpdate2.LogLines,
            l => Assert.Equal(LongLineCharacters, l.Text.Split(' ')[1].Length));

        resourceLoggerService.Complete("test-resource");
        await task.DefaultTimeout();
    }

    [Fact]
    public async Task WatchResources_ResourceHasCommands_CommandsSentWithResponse()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddXunit(testOutputHelper);
        });

        var logger = loggerFactory.CreateLogger<DashboardServiceTests>();
        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = new ResourceNotificationService(loggerFactory.CreateLogger<ResourceNotificationService>(), new TestHostApplicationLifetime(), new ServiceCollection().BuildServiceProvider(), resourceLoggerService);
        using var dashboardServiceData = new DashboardServiceData(resourceNotificationService, resourceLoggerService, loggerFactory.CreateLogger<DashboardServiceData>(), new DashboardCommandExecutor(new ServiceCollection().BuildServiceProvider()));
        var dashboardService = new DashboardService(dashboardServiceData, new TestHostEnvironment(), new TestHostApplicationLifetime(), loggerFactory.CreateLogger<DashboardService>());

        var testResource = new TestResource("test-resource");
        using var applicationBuilder = TestDistributedApplicationBuilder.Create(testOutputHelper: testOutputHelper);
        var builder = applicationBuilder.AddResource(testResource);
        builder.WithCommand(
            name: "TestName",
            displayName: "Display name!",
            executeCommand: c => Task.FromResult(CommandResults.Success()),
            updateState: c => ApplicationModel.ResourceCommandState.Enabled,
            displayDescription: "Display description!",
            parameter: new [] {"One", "Two"},
            confirmationMessage: "Confirmation message!",
            iconName: "Icon name!",
            iconVariant: ApplicationModel.IconVariant.Filled,
            isHighlighted: true);

        logger.LogInformation("Publishing resource.");
        await resourceNotificationService.PublishUpdateAsync(testResource, s =>
        {
            return s with { State = new ResourceStateSnapshot("Starting", null) };
        }).DefaultTimeout();

        logger.LogInformation("Waiting for the resource with a command. Required so added resource is always in the service's initial data collection");
        await dashboardServiceData.WaitForResourceAsync(testResource.Name, r =>
        {
            return r.Commands.Length == 1;
        }).DefaultTimeout();

        var cts = new CancellationTokenSource();
        var context = TestServerCallContext.Create(cancellationToken: cts.Token);
        var writer = new TestServerStreamWriter<WatchResourcesUpdate>(context);

        // Act
        logger.LogInformation("Calling WatchResources.");
        var task = dashboardService.WatchResources(
            new WatchResourcesRequest(),
            writer,
            context);

        // Assert
        logger.LogInformation("Reading result from writer.");
        var update = await writer.ReadNextAsync().DefaultTimeout();

        logger.LogInformation($"Initial data count: {update.InitialData.Resources.Count}");
        var resourceData = Assert.Single(update.InitialData.Resources);

        logger.LogInformation($"Commands count: {resourceData.Commands.Count}");
        var commandData = Assert.Single(resourceData.Commands);

        Assert.Equal("TestName", commandData.Name);
        Assert.Equal("Display name!", commandData.DisplayName);
        Assert.Equal("Display description!", commandData.DisplayDescription);
        Assert.Equal(Value.ForList(Value.ForString("One"), Value.ForString("Two")), commandData.Parameter);
        Assert.Equal("Confirmation message!", commandData.ConfirmationMessage);
        Assert.Equal("Icon name!", commandData.IconName);
        Assert.Equal(ResourceService.Proto.V1.IconVariant.Filled, commandData.IconVariant);
        Assert.True(commandData.IsHighlighted);

        await CancelTokenAndAwaitTask(cts, task).DefaultTimeout();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = default!;
        public IFileProvider ContentRootFileProvider { get; set; } = default!;
        public string ContentRootPath { get; set; } = default!;
        public string EnvironmentName { get; set; } = default!;
    }

    private sealed class TestResource(string name) : Resource(name)
    {
    }

    private static async Task CancelTokenAndAwaitTask(CancellationTokenSource cts, Task task)
    {
        await cts.CancelAsync();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Ok if this error is thrown.
        }
    }
}
