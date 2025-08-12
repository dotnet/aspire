// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class ContainerExecutableResourceTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task AddExecutableWithArgs()
    {
        var builder = TestDistributedApplicationBuilder.Create(outputHelper).WithTestAndResourceLogging(outputHelper);

        var containerResource = builder.AddResource(new TestContainerResource("testname"))
           .WithInitialState(new()
           {
               ResourceType = "TestProjectResource",
               State = new("Running", null),
               Properties = [new("A", "B"), new("c", "d")],
               EnvironmentVariables = [new("e", "f", true), new("g", "h", false)]
           })
           .WithImage("nginx")
           .WithImageTag("1.25");

        var exe = containerResource.AddContainerExecutable("test-exec", "ls", workingDirectory: ".");

        using var app = builder.Build();

        var startAppHost = app.StartAsync();

        var resourceLoggerService = app.Services.GetRequiredService<ResourceLoggerService>();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        var exeDcpName = exe.Resource.GetResolvedResourceName();

        _ = Task.Run(async () =>
        {
            await resourceNotificationService.WaitForResourceAsync(exe.Resource.Name, targetStates: KnownResourceStates.TerminalStates, CancellationToken.None);

            // hack: https://github.com/dotnet/aspire/issues/10245
            // workarounds the race-condition between streaming all logs from the resource, and resource completion
            await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);

            resourceLoggerService.Complete(exeDcpName); // complete stops the `WatchAsync` async-foreach below
        }, CancellationToken.None);

        List<string> logs = new();
        await foreach (var log in resourceLoggerService.WatchAsync(exeDcpName))
        {
            foreach (var entry in log)
            {
                logs.Add(entry.Content);
            }
        }

        foreach (var expectedLog in new [] { "bin", "boot", "dev" })
        {
            var logFound = logs.Any(x => x.Contains(expectedLog));
            Assert.True(logFound, $"Expected log message {expectedLog} not found in logs.");
        }
    }
}

file sealed class TestContainerResource : ContainerResource
{
    public TestContainerResource(string name) : base(name)
    {
    }
}
