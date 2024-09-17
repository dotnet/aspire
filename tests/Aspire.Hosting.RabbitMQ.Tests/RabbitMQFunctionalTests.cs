// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.RabbitMQ.Tests;

public class RabbitMQFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string RabbitMQReadyText = "Time to start RabbitMQ:";

    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnRabbitMQBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddRabbitMQ("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddRabbitMQ("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceAsync(resource.Resource.Name, (re => re.Snapshot.HealthStatus == HealthStatus.Healthy), cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyRabbitMQResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var rabbitMQ = builder.AddRabbitMQ("rabbitMQ");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration[$"ConnectionStrings:{rabbitMQ.Resource.Name}"] = await rabbitMQ.Resource.ConnectionStringExpression.GetValueAsync(default);
        hb.AddRabbitMQClient(rabbitMQ.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var connection = host.Services.GetRequiredService<IConnection>();

        using var channel = connection.CreateModel();
        const string queueName = "hello";
        channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        const string message = "Hello World!";
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: null, body: body);

        var result = channel.BasicGet(queueName, true);
        Assert.Equal(message, Encoding.UTF8.GetString(result.Body.Span));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var rabbitMQ1 = builder1.AddRabbitMQ("rabbitMQ");
            var password = rabbitMQ1.Resource.PasswordParameter.Value;

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(rabbitMQ1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                rabbitMQ1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;
                rabbitMQ1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();
                    hb.Configuration[$"ConnectionStrings:{rabbitMQ1.Resource.Name}"] = await rabbitMQ1.Resource.ConnectionStringExpression.GetValueAsync(default);
                    hb.Services.AddXunitLogging(testOutputHelper);
                    hb.AddRabbitMQClient(rabbitMQ1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();
                        await app.WaitForTextAsync(RabbitMQReadyText, resourceName: rabbitMQ1.Resource.Name).WaitAsync(TimeSpan.FromMinutes(1));

                        var connection = host.Services.GetRequiredService<IConnection>();

                        using var channel = connection.CreateModel();
                        const string queueName = "hello";
                        channel.QueueDeclare(queueName, durable: true, exclusive: false);

                        const string message = "Hello World!";
                        var body = Encoding.UTF8.GetBytes(message);

                        var props = channel.CreateBasicProperties();
                        props.Persistent = true; // or props.DeliveryMode = 2;
                        channel.BasicPublish(
                            exchange: string.Empty,
                            queueName,
                            props,
                            body);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }

            testOutputHelper.WriteLine($"Starting the second run with the same volume/mount");

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var passwordParameter2 = builder2.AddParameter("pwd");
            builder2.Configuration["Parameters:pwd"] = password;

            var rabbitMQ2 = builder2.AddRabbitMQ("rabbitMQ", password: passwordParameter2);

            if (useVolume)
            {
                rabbitMQ2.WithDataVolume(volumeName);
            }
            else
            {
                rabbitMQ2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();
                    hb.Configuration[$"ConnectionStrings:{rabbitMQ2.Resource.Name}"] = await rabbitMQ2.Resource.ConnectionStringExpression.GetValueAsync(default);
                    hb.Services.AddXunitLogging(testOutputHelper);
                    hb.AddRabbitMQClient(rabbitMQ2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();
                        await app.WaitForTextAsync(RabbitMQReadyText, resourceName: rabbitMQ2.Resource.Name).WaitAsync(TimeSpan.FromMinutes(1));

                        var connection = host.Services.GetRequiredService<IConnection>();

                        using var channel = connection.CreateModel();
                        const string queueName = "hello";
                        channel.QueueDeclare(queueName, durable: true, exclusive: false);

                        var result = channel.BasicGet(queueName, true);
                        Assert.Equal("Hello World!", Encoding.UTF8.GetString(result.Body.Span));
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }
        }
        finally
        {
            if (volumeName is not null)
            {
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
            }

            if (bindMountPath is not null)
            {
                try
                {
                    Directory.Delete(bindMountPath, recursive: true);
                }
                catch
                {
                    // Don't fail test if we can't clean the temporary folder
                }
            }
        }
    }
}
