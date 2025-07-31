// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;
using Aspire.Hosting.Exec;
using Aspire.Hosting.Testing;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests.Backchannel.Exec;

public class WithExecCommandTests : ExecTestsBase
{
    public WithExecCommandTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    [Fact]
    [RequiresDocker]
    public async Task WithExecCommand_NginxContainer_ListFiles_ProducesLogs_Success()
    {
        using var builder = PrepareBuilder(["--operation", "run"]);
        var (container, containerBuilder) = WithContainerWithExecCommand(builder);
        containerBuilder.WithExecCommand("list", "List files", "ls");

        var app = await EnsureAppStartAsync(builder);
        var containerExecService = app.Services.GetRequiredService<ContainerExecService>();

        // executing command on the container. We know it is running since DCP has already started.
        var executionLogs = containerExecService.ExecuteCommandAsync(container, "list", CancellationToken.None);
        var processedLogs = await ProcessAndCollectLogs(executionLogs);
        AssertLogsContain(processedLogs,
            "bin", "boot", "dev" // typical output of `ls` in a container
        );
    }

    private static (ContainerResource, IResourceBuilder<ContainerResource>) WithContainerWithExecCommand(IDistributedApplicationTestingBuilder builder, string name = "test")
    {
        var containerResource = new TestContainerResource(name);
        var contBuilder = builder.AddResource(containerResource)
               .WithInitialState(new()
               {
                   ResourceType = "TestProjectResource",
                   State = new("Running", null),
                   Properties = [new("A", "B"), new("c", "d")],
                   EnvironmentVariables = [new("e", "f", true), new("g", "h", false)]
               })
               .WithImage("nginx")
               .WithImageTag("1.25");

        return (containerResource, contBuilder);
    }

    /// <summary>
    /// Starts the apphost and waits for the resources to be created.
    /// </summary>
    private static async Task<DistributedApplication> EnsureAppStartAsync(IDistributedApplicationBuilder builder)
    {
        TaskCompletionSource<bool> resourcesCreated = new();

        using var app = builder.Build();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        var sub = eventing.Subscribe<AfterResourcesCreatedEvent>((afterResourcesCreatedEvent, token) =>
        {
            resourcesCreated.SetResult(true);
            return Task.CompletedTask;
        });

        _ = app.RunAsync();
        await resourcesCreated.Task;

        return app;
    }
}

file sealed class TestContainerResource : ContainerResource
{
    public TestContainerResource(string name) : base(name)
    {
    }
}
