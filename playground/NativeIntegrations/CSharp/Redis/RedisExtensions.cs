// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Native.Client;

namespace NativeIntegrations.Redis;

/// <summary>Builds a run-only Redis integration on shared declared containers and native symbolic values.</summary>
public static class RedisExtensions
{
    /// <summary>Adds a password-protected Redis container with authentic Redis protocol health.</summary>
    [AspireExport("addRedis")]
    public static async Task<RedisResource> AddRedis(Composition composition, string name, DeclaredValue password)
    {
        var resource = await composition.AddContainerAsync(name, "redis:7.4-alpine").ConfigureAwait(false);
        await resource.WithEntrypointAsync("/bin/sh").ConfigureAwait(false);
        await resource.WithEnvironmentAsync("REDIS_PASSWORD", password).ConfigureAwait(false);
        // Redis receives the password via its environment, not a resolved authoring
        // argument. Shell expansion is explicit and quoted, matching real hosting.
        foreach (var argument in new[] { "-c", "exec redis-server --requirepass \"$REDIS_PASSWORD\" --appendonly yes --appendfsync always --dir /data" })
        {
            await resource.WithArgumentAsync(await composition.CreateLiteralValueAsync(argument).ConfigureAwait(false)).ConfigureAwait(false);
        }
        await resource.WithEndpointAsync(new WorkloadEndpointDeclaration
        {
            Name = "tcp", Protocol = "TCP", UriScheme = "redis", TargetPort = 6379, HostPort = 0, EnvironmentVariable = ""
        }).ConfigureAwait(false);
        var host = await composition.CreateEndpointValueAsync(resource, "tcp", "host").ConfigureAwait(false);
        var port = await composition.CreateEndpointValueAsync(resource, "tcp", "port").ConfigureAwait(false);
        await resource.SetSymbolicConfigurationAsync("redis.health.host", host).ConfigureAwait(false);
        await resource.SetSymbolicConfigurationAsync("redis.health.port", port).ConfigureAwait(false);
        await resource.SetSymbolicConfigurationAsync("redis.health.password", password).ConfigureAwait(false);
        await resource.WithLifecycleExecutableAsync("health", new ExecutableWorkloadLaunch
        {
            ExecutablePath = Environment.ProcessPath ?? throw new InvalidOperationException("The integration provider needs an executable path."),
            WorkingDirectory = AppContext.BaseDirectory, Arguments = ["--redis-health"], Environment = [], Endpoints = []
        }, new SymbolicLaunchBindings
        {
            Environment =
            [
                new() { Name = "REDIS_HOST", ConfigurationKey = "redis.health.host" },
                new() { Name = "REDIS_PORT", ConfigurationKey = "redis.health.port" },
                new() { Name = "REDIS_PASSWORD", ConfigurationKey = "redis.health.password" }
            ],
            Arguments = []
        }).ConfigureAwait(false);
        var endpoint = await composition.CreateEndpointValueAsync(resource, "tcp", "hostAndPort").ConfigureAwait(false);
        var separator = await composition.CreateLiteralValueAsync(",password=").ConfigureAwait(false);
        var connection = await endpoint.ConcatValueAsync(separator).ConfigureAwait(false);
        connection = await connection.ConcatValueAsync(password).ConfigureAwait(false);

        return new RedisResource(resource, connection);
    }
}

/// <summary>Retains Redis's own API and symbolic connection expression over a native container declaration.</summary>
[AspireExport]
public sealed class RedisResource(DeclaredResource declaration, DeclaredValue connection) : NativeIntegrationResource(declaration)
{
    private CancellationTokenSource? _lifetime;

    /// <summary>Initializes provider-local runtime state without inspecting native credentials.</summary>
    [AspireExport("redisInitialize"), NativeResourceLifecycle("initialize")]
    public async Task Initialize(ResourceLifecycleScope scope, CancellationToken cancellationToken)
    {
        _lifetime = new CancellationTokenSource();
        await scope.WriteLifecycleLogAsync("stdout", "Redis integration initialized.", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Observes allocation using phase-scoped native authority, not a retained public declaration handle.</summary>
    [AspireExport("redisAllocated"), NativeResourceLifecycle("allocated")]
    public Task Allocated(ResourceLifecycleScope scope, CancellationToken cancellationToken) =>
        scope.WriteLifecycleLogAsync("stdout", "Redis endpoint allocated.", cancellationToken);

    /// <summary>Reports readiness only after native authenticated Redis health succeeds.</summary>
    [AspireExport("redisReady"), NativeResourceLifecycle("ready")]
    public Task Ready(ResourceLifecycleScope scope, CancellationToken cancellationToken) =>
        scope.WriteLifecycleLogAsync("stdout", "Redis authenticated and ready.", cancellationToken);

    /// <summary>Creates a final authenticated snapshot before native workload removal.</summary>
    [AspireExport("redisRetiring"), NativeResourceLifecycle("retiring")]
    public async Task Retiring(ResourceLifecycleScope scope, CancellationToken cancellationToken)
    {
        if (!await scope.RunLifecycleExecutableAsync(new ExecutableWorkloadLaunch
        {
            ExecutablePath = Environment.ProcessPath ?? throw new InvalidOperationException("The Redis provider needs an executable path."),
            WorkingDirectory = AppContext.BaseDirectory, Arguments = ["--redis-save"], Environment = [], Endpoints = []
        }, new SymbolicLaunchBindings
        {
            Environment =
            [
                new() { Name = "REDIS_HOST", ConfigurationKey = "redis.health.host" },
                new() { Name = "REDIS_PORT", ConfigurationKey = "redis.health.port" },
                new() { Name = "REDIS_PASSWORD", ConfigurationKey = "redis.health.password" }
            ],
            Arguments = []
        }, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Redis's final authenticated snapshot failed.");
        }
    }

    /// <summary>Releases provider-local state exactly once when native retirement acknowledges object disposal.</summary>
    public override ValueTask DisposeAsync()
    {
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;

        return ValueTask.CompletedTask;
    }

    /// <summary>Mounts a retained named Redis data volume at /data; AOF persistence is enabled by this bounded integration.</summary>
    [AspireExport("withDataVolume")]
    public async Task<RedisResource> WithDataVolume(string name)
    {
        await Declaration.WithVolumeAsync(name, "/data", false).ConfigureAwait(false);

        return this;
    }

    /// <summary>References this Redis connection from a host executable using native secret/reference resolution.</summary>
    [AspireExport("withReference")]
    public async Task<DeclaredResource> WithReference(DeclaredResource consumer, string environmentName)
    {
        await consumer.WithReferenceAsync(environmentName, connection).ConfigureAwait(false);

        return consumer;
    }
}
