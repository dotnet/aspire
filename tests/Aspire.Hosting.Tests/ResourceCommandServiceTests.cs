// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Hosting.Tests;

public class ResourceCommandServiceTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task ExecuteCommandAsync_NoMatchingResource_Failure()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var custom = builder.AddResource(new CustomResource("myResource"));

        var app = builder.Build();
        await app.StartAsync();

        // Act
        var result = await app.ResourceCommands.ExecuteCommandAsync("NotFoundResourceId", "NotFound");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Resource 'NotFoundResourceId' not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteCommandAsync_NoMatchingCommand_Failure()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var custom = builder.AddResource(new CustomResource("myResource"));

        var app = builder.Build();
        await app.StartAsync();

        // Act
        var result = await app.ResourceCommands.ExecuteCommandAsync(custom.Resource, "NotFound");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Command 'NotFound' not available for resource 'myResource'.", result.ErrorMessage);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9832")]
    public async Task ExecuteCommandAsync_HasReplicas_Success_CalledPerReplica()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var commandResourcesChannel = Channel.CreateUnbounded<string>();

        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithReplicas(2)
            .WithCommand(name: "mycommand",
                displayName: "My command",
                executeCommand: async e =>
                {
                    await commandResourcesChannel.Writer.WriteAsync(e.ResourceName);
                    return new ExecuteCommandResult { Success = true };
                });

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var result = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");
        commandResourcesChannel.Writer.Complete();

        // Assert
        Assert.True(result.Success);

        var resolvedResourceNames = resourceBuilder.Resource.GetResolvedResourceNames().ToList();
        await foreach (var resourceName in commandResourcesChannel.Reader.ReadAllAsync().DefaultTimeout())
        {
            Assert.True(resolvedResourceNames.Remove(resourceName));
        }

        commandResourcesChannel = Channel.CreateUnbounded<string>();
        foreach (var resourceName in resourceBuilder.Resource.GetResolvedResourceNames())
        {
            var r = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");
            Assert.True(r.Success);

            Assert.Equal(resourceName, await commandResourcesChannel.Reader.ReadAsync().DefaultTimeout());
        }
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9834")]
    public async Task ExecuteCommandAsync_HasReplicas_Failure_CalledPerReplica()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithReplicas(2)
            .WithCommand(name: "mycommand",
                displayName: "My command",
                executeCommand: e =>
                {
                    return Task.FromResult(new ExecuteCommandResult { Success = false, ErrorMessage = "Failure!" });
                });

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var result = await app.ResourceCommands.ExecuteCommandAsync(resourceBuilder.Resource, "mycommand");

        // Assert
        Assert.False(result.Success);

        var resourceNames = resourceBuilder.Resource.GetResolvedResourceNames();
        Assert.Equal($"""
            2 command executions failed.
            Resource '{resourceNames[0]}' failed with error message: Failure!
            Resource '{resourceNames[1]}' failed with error message: Failure!
            """, result.ErrorMessage);
    }

    private sealed class CustomResource(string name) : Resource(name), IResourceWithEndpoints, IResourceWithWaitSupport
    {

    }
}
