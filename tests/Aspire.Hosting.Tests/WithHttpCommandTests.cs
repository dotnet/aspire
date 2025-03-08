// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithHttpCommandTests
{
    private static readonly TimeSpan s_startTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_healthyTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public void WithHttpCommand_AddsResourceCommandAnnotation_WithDefaultValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing");

        // Act
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("Do The Thing", command.DisplayName);
        // Expected name format: "{endpoint.Resource.Name}-{endpoint.EndpointName}-http-{httpMethod}"
        Assert.Equal($"{resourceBuilder.Resource.Name}-http-http-post", command.Name);
        Assert.Null(command.DisplayDescription);
        Assert.Null(command.ConfirmationMessage);
        Assert.Null(command.IconName);
        Assert.Null(command.IconVariant);
        Assert.False(command.IsHighlighted);
    }

    [Fact]
    public void WithHttpCommand_AddsResourceCommandAnnotation_WithCustomValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing",
                commandName: "my-command-name",
                displayDescription: "Command description",
                confirmationMessage: "Are you sure?",
                iconName: "DatabaseLightning",
                iconVariant: IconVariant.Filled,
                isHighlighted: true);

        // Act
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("Do The Thing", command.DisplayName);
        Assert.Equal("my-command-name", command.Name);
        Assert.Equal("Command description", command.DisplayDescription);
        Assert.Equal("Are you sure?", command.ConfirmationMessage);
        Assert.Equal("DatabaseLightning", command.IconName);
        Assert.Equal(IconVariant.Filled, command.IconVariant);
        Assert.True(command.IsHighlighted);
    }

    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    [Theory]
    public async Task WithHttpCommand_ResultsInExpectedResultForStatusCode(int statusCode, bool expectSuccess)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand($"/status/{statusCode}", "Do The Thing", commandName: "mycommand");
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync().WaitAsync(s_startTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").WaitAsync(s_healthyTimeout);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.Equal(expectSuccess, result.Success);
    }

    [InlineData(null, false)] // Default method is POST
    [InlineData("get", true)]
    [InlineData("post", false)]
    [Theory]
    public async Task WithHttpCommand_ResultsInExpectedResultForHttpMethod(string? httpMethod, bool expectSuccess)
    {
        // Arrange
        var method = httpMethod is not null ? new HttpMethod(httpMethod) : null;
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/get-only", "Do The Thing", method: method, commandName: "mycommand");
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync().WaitAsync(s_startTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").WaitAsync(s_healthyTimeout);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.Equal(expectSuccess, result.Success);
    }

    [Fact]
    public async Task WithHttpCommand_UsesNamedHttpClient()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var trackingMessageHandler = new TrackingHttpMessageHandler();
        builder.Services.AddHttpClient("commandclient")
            .AddHttpMessageHandler((sp) => trackingMessageHandler);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/get-only", "Do The Thing", commandName: "mycommand", httpClientName: "commandclient");
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync().WaitAsync(s_startTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").WaitAsync(s_healthyTimeout);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.True(trackingMessageHandler.Called);
    }

    private sealed class TrackingHttpMessageHandler : DelegatingHandler
    {
        public bool Called { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Called = true;
            return base.SendAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task WithHttpCommand_CallsPrepareRequestCallback_BeforeSendingRequest()
    {
        // Arrange
        var callbackCalled = false;
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing",
                commandName: "mycommand",
                configureRequest: hrm =>
                {
                    callbackCalled = true;
                    return Task.CompletedTask;
                });
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync().WaitAsync(s_startTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").WaitAsync(s_healthyTimeout);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.True(callbackCalled);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task WithHttpCommand_CallsGetResponseCallback_AfterSendingRequest()
    {
        // Arrange
        var callbackCalled = false;
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing",
                commandName: "mycommand",
                getCommandResult: response =>
                {
                    callbackCalled = true;
                    return Task.FromResult(CommandResults.Failure("A test error message"));
                });
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync().WaitAsync(s_startTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").WaitAsync(s_healthyTimeout);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.True(callbackCalled);
        Assert.False(result.Success);
        Assert.Equal("A test error message", result.ErrorMessage);
    }

    [Fact]
    public async Task WithHttpCommand_EnablesCommandOnceResourceIsRunning()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        var markHealthy = false;
        builder.Services.AddHealthChecks()
            .AddCheck("ManualHealthCheck", () => markHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Not ready"));

        builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing", commandName: "mycommand")
            .WithHealthCheck("ManualHealthCheck");

        using var app = builder.Build();
        ResourceCommandState? commandState = null;
        var watchCts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(watchCts.Token).WithCancellation(watchCts.Token))
            {
                var commandSnapshot = resourceEvent.Snapshot.Commands.First(c => c.Name == "mycommand");
                commandState = commandSnapshot.State;
            }
        }, watchCts.Token);

        // Act/Assert
        await app.StartAsync().WaitAsync(s_startTimeout);

        await app.ResourceNotifications.WaitForResourceAsync("servicea", KnownResourceStates.Starting).WaitAsync(s_startTimeout);

        Assert.Equal(ResourceCommandState.Disabled, commandState);

        markHealthy = true;

        var resourceEvent = await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").WaitAsync(s_healthyTimeout);

        Assert.Equal(ResourceCommandState.Enabled, commandState);

        // Clean up
        watchCts.Cancel();
        await app.StopAsync();
    }
}
