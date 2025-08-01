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
    public async Task WithExecCommand_NginxContainer_ListFiles_WatchLogStream_Success()
    {
        using var builder = PrepareBuilder(["--operation", "run"]);
        var (container, containerBuilder) = WithContainerWithExecCommand(builder);
        containerBuilder.WithExecCommand("list", "List files", "ls");

        var app = await EnsureAppStartAsync(builder);
        var containerExecService = app.Services.GetRequiredService<IContainerExecService>();

        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // executing command on the container. We know it is running since DCP has already started.
        var execCommandRun = containerExecService.ExecuteCommand(container, "list");
        var runCommandTask = execCommandRun.ExecuteCommand(cancellationTokenSource.Token);

        // the option here is either to execute the command, and collect logs later;
        // or to run the command and immediately attach to the output stream. This will make
        // the logs to be streamed in parallel with the command execution.
        var output = execCommandRun.GetOutputStream(cancellationTokenSource.Token);
        var processedLogs = await ProcessAndCollectLogs(output);

        var result = await runCommandTask;
        Assert.True(result.Success);

        AssertLogsContain(processedLogs,
            "bin", "boot", "dev" // typical output of `ls` in a container
        );
    }

    [Fact]
    [RequiresDocker]
    public async Task WithExecCommand_NginxContainer_ListFiles_GetsAllLogs_Success()
    {
        using var builder = PrepareBuilder(["--operation", "run"]);
        var (container, containerBuilder) = WithContainerWithExecCommand(builder);
        containerBuilder.WithExecCommand("list", "List files", "ls");

        var app = await EnsureAppStartAsync(builder);
        var containerExecService = app.Services.GetRequiredService<IContainerExecService>();

        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // executing command on the container. We know it is running since DCP has already started.
        var execCommandRun = containerExecService.ExecuteCommand(container, "list");
        var result = await execCommandRun.ExecuteCommand(cancellationTokenSource.Token);
        Assert.True(result.Success);

        var output = execCommandRun.GetOutputStream(cancellationTokenSource.Token);
        var processedLogs = await ProcessAndCollectLogs(output);
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

        var app = builder.Build();
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
