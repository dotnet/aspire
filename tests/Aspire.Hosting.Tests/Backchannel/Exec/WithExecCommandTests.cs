// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public async Task Exec_NginxContainer_ListFiles_ProducesLogs_Success()
    {
        using var builder = PrepareBuilder([]);
        var (container, containerBuilder) = WithContainerWithExecCommand(builder);
        containerBuilder.WithExecCommand("list", "List files", "ls");

        using var app = builder.Build();

        var containerExecService = app.Services.GetRequiredService<ContainerExecService>();

        // act here
        await containerExecService.ExecuteCommandAsync(container, "list", CancellationToken.None);

        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(60));
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
}

file sealed class TestContainerResource : ContainerResource
{
    public TestContainerResource(string name) : base(name)
    {
    }
}
