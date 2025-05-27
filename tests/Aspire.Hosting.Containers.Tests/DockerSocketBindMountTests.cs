// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Hosting.Containers.Tests;

public class DockerSocketBindMountTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task WithDockerSocketBindMountAllowsDockerCliInContainer()
    {
        // TODO: Change this to the acr
        var dockerfile = """
            FROM docker:latest
            CMD ["docker", "info"]
            """;

        using var dir = new TempDirectory();
        var dockerFilePath = Path.Combine(dir.Path, "Dockerfile");
        await File.WriteAllTextAsync(dockerFilePath, dockerfile);

        var appBuilder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        appBuilder.AddDockerfile("docker-client", contextPath: dir.Path)
                  .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock");

        using var app = appBuilder.Build();

        await app.StartAsync();

        var rns = app.ResourceNotifications;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var state = await rns.WaitForResourceAsync("docker-client", e => KnownResourceStates.TerminalStates.Contains(e.Snapshot.State?.Text), cts.Token);

        Assert.Equal(KnownResourceStates.Exited, state.Snapshot.State);
    }
}