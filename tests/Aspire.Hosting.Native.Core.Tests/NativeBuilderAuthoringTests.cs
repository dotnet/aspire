// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias RedisContracts;

using System.Net;
using System.Net.Sockets;
using Aspire.Hosting.Native.Authoring;
using Aspire.Hosting.Native.Client;
using Aspire.Hosting.Native.Core.Tests.TestServices;
using NativeIntegrations.Redis;
using static RedisContracts::NativeIntegrations.Redis.AppHost.RedisBuilderExtensions;

namespace Aspire.Hosting.Native.Core.Tests;

public class NativeBuilderAuthoringTests
{
    [Fact]
    public void OrdinaryProviderAuthoringCancellationIsInvocationContextNotAnExtraWireArgument()
    {
        var contract = RedisContracts::NativeIntegration.Generated.ProviderContract.Create();
        var capability = Assert.Single(contract.Metadata.GetProperty("capabilities").EnumerateArray(),
            entry => entry.GetProperty("id").GetString() == "example.csharp.redis/addRedis");
        Assert.Equal(["composition", "name", "password"], capability.GetProperty("parameters").EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString()));
        var projected = typeof(RedisContracts::NativeIntegration.Generated.IntegrationExtensions)
            .GetMethod("AddRedisAsync")!;
        Assert.Equal([typeof(Composition), typeof(string), typeof(DeclaredValue), typeof(CancellationToken)],
            projected.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void FamiliarTypedCompositionAndBuildAreSynchronousAndPerformNoRpc()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var builder = DistributedApplication.CreateBuilder([$"--native-endpoint={listener.LocalEndpoint}"]);
        IResourceBuilder<RedisContainerResource> cache = builder.AddRedis("cache").WithDataVolume();
        IResourceBuilder<ExecutableResource> consumer = builder.AddExecutable("consumer", "unused-executable", ".")
            .WithReference(cache).WaitFor(cache);
        var application = builder.Build();
        Assert.IsType<RedisContainerResource>(cache.Resource);
        Assert.IsType<ExecutableResource>(consumer.Resource);
        Assert.Same(builder, cache.ApplicationBuilder);
        Assert.Same(builder, consumer.ApplicationBuilder);
        Assert.IsType<NativeApplication>(application);
        Assert.False(listener.Pending());
        Assert.False(typeof(NativeRemoteObject).IsAssignableFrom(cache.Resource.GetType()));
        Assert.Throws<InvalidOperationException>(() => cache.WithDataVolume("after-build"));
        Assert.Throws<InvalidOperationException>(() => builder.AddRedis("after-build"));
    }

    [Fact]
    public async Task CancelledRunDoesNotConnectAndDisposalCallersJoinExactlyOnce()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var builder = DistributedApplication.CreateBuilder([$"--native-endpoint={listener.LocalEndpoint}"]);
        var application = builder.Build();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await application.RunAsync(cancellation.Token);
        var first = application.DisposeAsync().AsTask();
        var second = application.DisposeAsync().AsTask();
        Assert.Same(first, second);
        await Task.WhenAll(first, second);
        Assert.False(listener.Pending());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => application.StartAsync());
    }

    [Fact]
    public void ReferencesRejectForeignTransportPlansWithoutConnecting()
    {
        var first = DistributedApplication.CreateBuilder([]);
        var second = DistributedApplication.CreateBuilder([]);
        var cache = first.AddRedis("cache");
        var consumer = second.AddExecutable("consumer", "unused-executable", ".");
        Assert.Throws<ArgumentException>(() => consumer.WithReference(cache));
        Assert.Throws<ArgumentException>(() => consumer.WaitFor(cache));
        Assert.Throws<NotSupportedException>(() => DistributedApplication.CreateBuilder(["--publish=true"]));
    }

    [Fact]
    public async Task IntegrationAuthorsComposeGenericContainersAndNativeValuesWithoutStartingWorkloads()
    {
        await using var host = await NativeClientTestHost.StartAsync();
        await using var native = await NativeClient.ConnectAsync(host.Endpoint, host.Token, TimeSpan.FromSeconds(10), TimeProvider.System);
        var session = await native.Server.OpenApplicationAsync();
        var composition = await session.StartGenerationAsync();
        var builder = DistributedApplicationBuilder.ForIntegration();
        var cache = NativeIntegrations.Redis.Authoring.RedisBuilderExtensions.AddRedis(builder, "cache").WithDataVolume("owned-volume");
        var consumer = builder.AddExecutable("consumer", Environment.ProcessPath!, AppContext.BaseDirectory).WithReference(cache);
        var waiter = builder.AddExecutable("waiter", Environment.ProcessPath!, AppContext.BaseDirectory).WaitFor(cache);
        var bound = await builder.MaterializeAsync(composition, CancellationToken.None);
        var snapshot = await composition.InspectAsync();
        Assert.Equal(["cache", "consumer", "waiter"], snapshot.Resources.Select(resource => resource.Name));
        var producer = snapshot.Resources[0];
        Assert.Equal([producer.ResourceId], snapshot.Resources[1].Dependencies);
        Assert.Equal([producer.ResourceId], snapshot.Resources[2].Dependencies);
        var consumerValues = (await bound.Declaration(consumer.Resource).InspectSymbolicConfigurationAsync()).Properties;
        Assert.True(Assert.Single(consumerValues, property => property.Name.Contains("ConnectionStrings__cache", StringComparison.Ordinal)).IsSecret);
        Assert.Empty((await bound.Declaration(waiter.Resource).InspectSymbolicConfigurationAsync()).Properties);
        Assert.True((await bound.Declaration(cache.Resource).InspectSymbolicConfigurationAsync()).Properties
            .Single(property => property.Name == "health.env.REDIS_PASSWORD").IsSecret);
        Assert.Equal("[native reference expression]", cache.Resource.ConnectionStringExpression.ToString());
        await session.RetireGenerationAsync(composition);
    }

    [Fact]
    public async Task GeneratedCredentialIsNativeOwnedAndRevokedWithItsGeneration()
    {
        await using var host = await NativeClientTestHost.StartAsync();
        await using var native = await NativeClient.ConnectAsync(host.Endpoint, host.Token, TimeSpan.FromSeconds(10), TimeProvider.System);
        var session = await native.Server.OpenApplicationAsync();
        var composition = await session.StartGenerationAsync();
        var password = await composition.AddGeneratedSecretParameterAsync("generated");
        var resource = await composition.AddResourceAsync("target", "example/target");
        await resource.SetSymbolicConfigurationAsync("password", password);
        var property = Assert.Single((await resource.InspectSymbolicConfigurationAsync()).Properties);
        Assert.True(property.IsSecret);
        Assert.Empty((await resource.InspectResourceAsync()).Configuration);
        await Assert.ThrowsAsync<NativeCapabilityException>(() => composition.AddGeneratedSecretParameterAsync("generated"));
        await session.RetireGenerationAsync(composition);
        var failure = await Assert.ThrowsAsync<NativeCapabilityException>(() => password.EncodeUriValueAsync());
        Assert.Equal("HANDLE_NOT_FOUND", failure.Code);
    }
}
