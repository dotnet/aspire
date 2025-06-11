// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithHttpCommandTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void WithHttpCommand_AddsHttpClientFactory()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing");

        using var app = builder.Build();

        // Assert
        var httpClientFactoryServiceDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(IHttpClientFactory));
        Assert.NotNull(httpClientFactoryServiceDescriptor);

        var httpClientFactory = app.Services.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void WithHttpCommand_Throws_WhenEndpointByNameIsNotHttp()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var container = builder.AddContainer("name", "image")
                .WithEndpoint(targetPort: 9999, scheme: "tcp", name: "nonhttp");

        // Act
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            container.WithHttpCommand("/some-path", "Do The Thing", endpointName: "nonhttp");
        });

        // Assert
        Assert.Equal(
            "Could not create HTTP command for resource 'name' as the endpoint with name 'nonhttp' and scheme 'tcp' is not an HTTP endpoint.",
            ex.Message
        );
    }

    [Fact]
    public void WithHttpCommand_Throws_WhenEndpointIsNotHttp()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var container = builder.AddContainer("name", "image")
                .WithEndpoint(targetPort: 9999, scheme: "tcp", name: "nonhttp");

        // Act
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            container.WithHttpCommand("/some-path", "Do The Thing", () => container.GetEndpoint("nonhttp"));
        });

        // Assert
        Assert.Equal(
            "Could not create HTTP command for resource 'name' as the endpoint with name 'nonhttp' and scheme 'tcp' is not an HTTP endpoint.",
            ex.Message
        );
    }

    [Fact]
    public void WithHttpCommand_AddsResourceCommandAnnotation_WithDefaultValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing");

        // Act
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("Do The Thing", command.DisplayName);
        // Expected name format: "{endpoint.Resource.Name}-{endpoint.EndpointName}-http-{httpMethod}"
        Assert.Equal($"{resourceBuilder.Resource.Name}-http-http-post-/some-path", command.Name);
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
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing",
                commandName: "my-command-name",
                commandOptions: new()
                {
                    Description = "Command description",
                    ConfirmationMessage = "Are you sure?",
                    IconName = "DatabaseLightning",
                    IconVariant = IconVariant.Filled,
                    IsHighlighted = true
                });

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

    [Fact]
    public void WithHttpCommand_AddsResourceCommandAnnotations_WithUniqueCommandNames()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpEndpoint(name: "custom-endpoint")
            .WithHttpCommand("/some-path", "Do The Thing")
            .WithHttpCommand("/some-path", "Do The Thing", endpointName: "custom-endpoint")
            .WithHttpCommand("/some-path", "Do The Get Thing", commandOptions: new() { Method = HttpMethod.Get })
            .WithHttpCommand("/some-path", "Do The Get Thing", endpointName: "custom-endpoint", commandOptions: new() { Method = HttpMethod.Get })
            .WithHttpCommand("/some-other-path", "Do The Other Thing")
            // Call it again but just change display name, it should override the previous one with the same path
            .WithHttpCommand("/some-other-path", "Do The Other Thing CHANGED");

        // Act
        var commands = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().ToList();
        var command1 = commands.FirstOrDefault(c => c.DisplayName == "Do The Thing");
        var command2 = commands.FirstOrDefault(c => c.DisplayName == "Do The Thing" && c.Name.Contains("custom-endpoint"));
        var command3 = commands.FirstOrDefault(c => c.DisplayName == "Do The Get Thing");
        var command4 = commands.FirstOrDefault(c => c.DisplayName == "Do The Get Thing" && c.Name.Contains("custom-endpoint"));
        var command5 = commands.FirstOrDefault(c => c.DisplayName == "Do The Other Thing");
        var command6 = commands.FirstOrDefault(c => c.DisplayName == "Do The Other Thing CHANGED");

        // Assert
        Assert.True(commands.Count >= 5);
        Assert.NotNull(command1);
        Assert.NotNull(command2);
        Assert.NotNull(command3);
        Assert.NotNull(command4);
        Assert.Null(command5); // This one is overridden by the last one
        Assert.NotNull(command6);
    }

    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9670")]
    [Theory]
    public async Task WithHttpCommand_ResultsInExpectedResultForStatusCode(int statusCode, bool expectSuccess)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand($"/status/{statusCode}", "Do The Thing", commandName: "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var result = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");

        // Assert
        Assert.Equal(expectSuccess, result.Success);
    }

    [InlineData(null, false)] // Default method is POST
    [InlineData("get", true)]
    [InlineData("post", false)]
    [Theory]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/9725")]
    public async Task WithHttpCommand_ResultsInExpectedResultForHttpMethod(string? httpMethod, bool expectSuccess)
    {
        // Arrange
        var method = httpMethod is not null ? new HttpMethod(httpMethod) : HttpCommandOptions.Default.Method;
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/get-only", "Do The Thing", commandName: "mycommand", commandOptions: new() { Method = method });

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var result = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");

        // Assert
        Assert.Equal(expectSuccess, result.Success);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9800")]
    public async Task WithHttpCommand_UsesNamedHttpClient()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var trackingMessageHandler = new TrackingHttpMessageHandler();
        builder.Services.AddHttpClient("commandclient")
            .AddHttpMessageHandler((sp) => trackingMessageHandler);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/get-only", "Do The Thing", commandName: "mycommand", commandOptions: new() { HttpClientName = "commandclient" });

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var result = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");

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
    public async Task WithHttpCommand_UsesEndpointSelector()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var serviceA = builder.AddProject<Projects.ServiceA>("servicea");
        var callbackCalled = false;
        var serviceB = builder.AddProject<Projects.ServiceA>("serviceb")
            .WithHttpCommand("/status/200", "Do The Thing", commandName: "mycommand",
                endpointSelector: () =>
                {
                    callbackCalled = true;
                    return serviceA.GetEndpoint("http");
                });

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var result = await app.ResourceCommands.ExecuteCommandAsync(serviceB.Resource, "mycommand");

        // Assert
        Assert.True(callbackCalled);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9789")]
    public async Task WithHttpCommand_CallsPrepareRequestCallback_BeforeSendingRequest()
    {
        // Arrange
        var callbackCalled = false;
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        string resolvedResourceName = null!;
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing",
                commandName: "mycommand",
                commandOptions: new()
                {
                    PrepareRequest = requestContext =>
                    {
                        Assert.NotNull(requestContext);
                        Assert.NotNull(requestContext.ServiceProvider);
                        Assert.Equal(resolvedResourceName, requestContext.ResourceName);
                        Assert.NotNull(requestContext.Endpoint);
                        Assert.NotNull(requestContext.HttpClient);
                        Assert.NotNull(requestContext.Request);

                        callbackCalled = true;
                        return Task.CompletedTask;
                    }
                });

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        resolvedResourceName = resourceBuilder.Resource.GetResolvedResourceNames().Single();

        var result = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");

        // Assert
        Assert.True(callbackCalled);
        Assert.True(result.Success);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9772")]
    public async Task WithHttpCommand_CallsGetResponseCallback_AfterSendingRequest()
    {
        // Arrange
        var callbackCalled = false;
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        string resolvedResourceName = null!;
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing",
                commandName: "mycommand",
                commandOptions: new()
                {
                    GetCommandResult = resultContext =>
                    {
                        Assert.NotNull(resultContext);
                        Assert.NotNull(resultContext.ServiceProvider);
                        Assert.Equal(resolvedResourceName, resultContext.ResourceName);
                        Assert.NotNull(resultContext.Endpoint);
                        Assert.NotNull(resultContext.HttpClient);
                        Assert.NotNull(resultContext.Response);

                        callbackCalled = true;
                        return Task.FromResult(CommandResults.Failure("A test error message"));
                    }
                });

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        resolvedResourceName = resourceBuilder.Resource.GetResolvedResourceNames().Single();

        var result = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");

        // Assert
        Assert.True(callbackCalled);
        Assert.False(result.Success);
        Assert.Equal("A test error message", result.ErrorMessage);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/8101")]
    public async Task WithHttpCommand_EnablesCommandOnceResourceIsRunning()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        builder.Configuration["CODESPACES"] = "false";

        var service = builder.AddResource(new CustomResource("service"))
            .WithHttpEndpoint()
            .WithHttpCommand("/dothing", "Do The Thing", commandName: "mycommand");

        using var app = builder.Build();
        ResourceCommandState? commandState = null;
        var watchTcs = new TaskCompletionSource();
        var watchCts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(watchCts.Token).WithCancellation(watchCts.Token))
            {
                var commandSnapshot = resourceEvent.Snapshot.Commands.First(c => c.Name == "mycommand");
                commandState = commandSnapshot.State;
                if (commandState == ResourceCommandState.Enabled)
                {
                    watchTcs.TrySetResult();
                }
            }
        }, watchCts.Token);

        // Act/Assert
        await app.StartAsync();

        // Move the resource to the starting state
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Starting
        });
        await app.ResourceNotifications.WaitForResourceAsync(service.Resource.Name, KnownResourceStates.Starting).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Veriy the command is disabled
        Assert.Equal(ResourceCommandState.Disabled, commandState);

        // Move the resource to the running state
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });
        await app.ResourceNotifications.WaitForResourceAsync(service.Resource.Name, KnownResourceStates.Running).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        await watchTcs.Task.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Verify the command is enabled
        Assert.Equal(ResourceCommandState.Enabled, commandState);

        // Clean up
        watchCts.Cancel();
        await app.StopAsync();
    }

    [Fact]
    public async Task WithHttpCommand_EnablesCommandUsingCustomUpdateStateCallback()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        builder.Configuration["CODESPACES"] = "false";

        var enableCommand = false;
        var callbackCalled = false;
        var service = builder.AddResource(new CustomResource("service"))
            .WithHttpEndpoint()
            .WithHttpCommand("/dothing", "Do The Thing",
                commandName: "mycommand",
                commandOptions: new()
                {
                    UpdateState = usc =>
                    {
                        callbackCalled = true;
                        return enableCommand ? ResourceCommandState.Enabled : ResourceCommandState.Hidden;
                    }
                });

        using var app = builder.Build();
        ResourceCommandState? commandState = null;
        var watchTcs = new TaskCompletionSource();
        var watchCts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(watchCts.Token).WithCancellation(watchCts.Token))
            {
                var commandSnapshot = resourceEvent.Snapshot.Commands.First(c => c.Name == "mycommand");
                commandState = commandSnapshot.State;
                if (commandState == ResourceCommandState.Enabled)
                {
                    watchTcs.TrySetResult();
                }
            }
        }, watchCts.Token);

        // Act/Assert
        await app.StartAsync();

        // Move the resource to the running state
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });
        await app.ResourceNotifications.WaitForResourceAsync(service.Resource.Name, KnownResourceStates.Running).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Veriy the command is hidden despite the resource being running
        Assert.Equal(ResourceCommandState.Hidden, commandState);

        // Publish an update to force reevaluation of the command state
        enableCommand = true;
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });
        await watchTcs.Task.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Verify the callback was called and the command is enabled
        Assert.True(callbackCalled);
        Assert.Equal(ResourceCommandState.Enabled, commandState);

        // Clean up
        watchCts.Cancel();
        await app.StopAsync();
    }

    private sealed class CustomResource(string name) : Resource(name), IResourceWithEndpoints, IResourceWithWaitSupport
    {

    }
}
