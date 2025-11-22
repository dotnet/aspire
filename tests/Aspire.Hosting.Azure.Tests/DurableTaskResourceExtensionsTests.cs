// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class DurableTaskResourceExtensionsTests
{
    [Fact]
    public async Task AddDurableTaskScheduler_RunAsEmulator_ResolvedConnectionString()
    {
        string expectedConnectionString = "Endpoint=http://localhost:8080;Authentication=None";

        using var builder = TestDistributedApplicationBuilder.Create();

        var dts = builder
            .AddDurableTaskScheduler("dts")
            .RunAsEmulator(e =>
            {
                e.WithEndpoint("grpc", e => e.AllocatedEndpoint = new(e, "localhost", 8080));
                e.WithEndpoint("http", e => e.AllocatedEndpoint = new(e, "localhost", 8081));
                e.WithEndpoint("dashboard", e => e.AllocatedEndpoint = new(e, "localhost", 8082));
            });

        var connectionString = await dts.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Equal(expectedConnectionString, connectionString);
    }

    [Fact]
    public async Task AddDurableTaskScheduler_RunAsExisting_ResolvedConnectionString()
    {
        string expectedConnectionString = "Endpoint=https://existing-scheduler.durabletask.io;Authentication=DefaultAzure";

        using var builder = TestDistributedApplicationBuilder.Create();

        var dts = builder
            .AddDurableTaskScheduler("dts")
            .RunAsExisting(expectedConnectionString);

        var connectionString = await dts.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Equal(expectedConnectionString, connectionString);
    }

    [Fact]
    public async Task AddDurableTaskScheduler_RunAsExisting_ResolvedConnectionStringParameter()
    {
        string expectedConnectionString = "Endpoint=https://existing-scheduler.durabletask.io;Authentication=DefaultAzure";

        using var builder = TestDistributedApplicationBuilder.Create();

        var connectionStringParameter = builder.AddParameter("dts-connection-string", expectedConnectionString);

        var dts = builder
            .AddDurableTaskScheduler("dts")
            .RunAsExisting(connectionStringParameter);

        var connectionString = await dts.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Equal(expectedConnectionString, connectionString);
    }

    [Theory]
    [InlineData(null, "mytaskhub")]
    [InlineData("myrealtaskhub", "myrealtaskhub")]
    public async Task AddDurableTaskHub_RunAsExisting_ResolvedConnectionStringParameter(string? taskHubName, string expectedTaskHubName)
    {
        string dtsConnectionString = "Endpoint=https://existing-scheduler.durabletask.io;Authentication=DefaultAzure";
        string expectedConnectionString = $"{dtsConnectionString};TaskHub={expectedTaskHubName}";
        using var builder = TestDistributedApplicationBuilder.Create();

        var connectionStringParameter = builder.AddParameter("dts-connection-string", expectedConnectionString);

        var dts = builder
            .AddDurableTaskScheduler("dts")
            .RunAsExisting(dtsConnectionString);

        var taskHub = dts.AddTaskHub("mytaskhub", taskHubName);

        var connectionString = await taskHub.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Equal(expectedConnectionString, connectionString);
    }
}
