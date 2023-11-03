// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

/// <summary>
/// Tests for <see cref="ServiceEndPointResolverFactory"/> and <see cref="ServiceEndPointResolver"/>.
/// </summary>
public class ServiceEndPointResolverTests
{
    [Fact]
    public void ResolveServiceEndPoint_NoResolversConfigured_Throws()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();
        var exception = Assert.Throws<InvalidOperationException>(() => resolverFactory.CreateResolver("https://basket"));
        Assert.Equal("No resolver which supports the provided service name, 'https://basket', has been configured.", exception.Message);
    }

    [Fact]
    public async Task ServiceEndPointResolver_NoResolversConfigured_Throws()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverFactory = new ServiceEndPointResolver([], NullLogger.Instance, "foo", TimeProvider.System, Options.Options.Create(new ServiceEndPointResolverOptions()));
        var exception = Assert.Throws<InvalidOperationException>(resolverFactory.Start);
        Assert.Equal("No service endpoint resolvers are configured.", exception.Message);
        exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await resolverFactory.GetEndPointsAsync());
        Assert.Equal("No service endpoint resolvers are configured.", exception.Message);
    }

    [Fact]
    public void ResolveServiceEndPoint_NullServiceName_Throws()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();
        Assert.Throws<ArgumentNullException>(() => resolverFactory.CreateResolver(null!));
    }

    [Fact]
    public async Task UseServiceDiscovery_NoResolvers_Throws()
    {
        var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("foo", c => c.BaseAddress = new("http://foo"))
            .UseServiceDiscovery();
        var services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("foo");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetStringAsync("/"));
        Assert.Equal("No resolver which supports the provided service name, 'http://foo', has been configured.", exception.Message);
    }

    private sealed class FakeEndPointResolverProvider(Func<string, (bool Result, IServiceEndPointResolver? Resolver)> createResolverDelegate) : IServiceEndPointResolverProvider
    {
        public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver)
        {
            bool result;
            (result, resolver) = createResolverDelegate(serviceName);
            return result;
        }
    }

    private sealed class FakeEndPointResolver(Func<ServiceEndPointCollectionSource, CancellationToken, ValueTask<ResolutionStatus>> resolveAsync, Func<ValueTask> disposeAsync) : IServiceEndPointResolver
    {
        public string DisplayName => "Fake";

        public ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken) => resolveAsync(endPoints, cancellationToken);
        public ValueTask DisposeAsync() => disposeAsync();
    }

    [Fact]
    public async Task ResolveServiceEndPoint()
    {
        var cts = new[] { new CancellationTokenSource() };
        var innerResolver = new FakeEndPointResolver(
            resolveAsync: (collection, ct) =>
            {
                collection.AddChangeToken(new CancellationChangeToken(cts[0].Token));
                collection.EndPoints.Add(ServiceEndPoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080)));

                if (cts[0].Token.IsCancellationRequested)
                {
                    cts[0] = new();
                    collection.EndPoints.Add(ServiceEndPoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.2"), 8888)));
                }
                return default;
            },
            disposeAsync: () => default);
        var resolverProvider = new FakeEndPointResolverProvider(name => (true, innerResolver));
        var services = new ServiceCollection()
            .AddSingleton<IServiceEndPointResolverProvider>(resolverProvider)
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();

        ServiceEndPointResolver resolver;
        await using ((resolver = resolverFactory.CreateResolver("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var initialEndPoints = await resolver.GetEndPointsAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(initialEndPoints);
            var sep = Assert.Single(initialEndPoints);
            var ip = Assert.IsType<IPEndPoint>(sep.EndPoint);
            Assert.Equal(IPAddress.Parse("127.1.1.1"), ip.Address);
            Assert.Equal(8080, ip.Port);

            var tcs = new TaskCompletionSource<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = tcs.SetResult;
            Assert.False(tcs.Task.IsCompleted);

            cts[0].Cancel();
            var resolverResult = await tcs.Task.ConfigureAwait(false);
            Assert.NotNull(resolverResult);
            Assert.Equal(ResolutionStatus.Success, resolverResult.Status);
            Assert.True(resolverResult.ResolvedSuccessfully);
            Assert.Equal(2, resolverResult.EndPoints.Count);
            var endpoints = resolverResult.EndPoints.Select(ep => ep.EndPoint).OfType<IPEndPoint>().ToList();
            endpoints.Sort((l, r) => l.Port - r.Port);
            Assert.Equal(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080), endpoints[0]);
            Assert.Equal(new IPEndPoint(IPAddress.Parse("127.1.1.2"), 8888), endpoints[1]);
        }
    }

    [Fact]
    public async Task ResolveServiceEndPoint_ThrowOnReload()
    {
        var sem = new SemaphoreSlim(0);
        var cts = new[] { new CancellationTokenSource() };
        var throwOnNextResolve = new[] { false };
        var innerResolver = new FakeEndPointResolver(
            resolveAsync: async (collection, ct) =>
            {
                await sem.WaitAsync(ct).ConfigureAwait(false);
                if (cts[0].IsCancellationRequested)
                {
                    // Always be sure to have a fresh token.
                    cts[0] = new();
                }

                if (throwOnNextResolve[0])
                {
                    throwOnNextResolve[0] = false;
                    throw new InvalidOperationException("throwing");
                }

                collection.AddChangeToken(new CancellationChangeToken(cts[0].Token));
                collection.EndPoints.Add(ServiceEndPoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080)));
                return ResolutionStatus.Success;
            },
            disposeAsync: () => default);
        var resolverProvider = new FakeEndPointResolverProvider(name => (true, innerResolver));
        var services = new ServiceCollection()
            .AddSingleton<IServiceEndPointResolverProvider>(resolverProvider)
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndPointResolverFactory>();

        ServiceEndPointResolver resolver;
        await using ((resolver = resolverFactory.CreateResolver("http://basket")).ConfigureAwait(false))
        {
            Assert.NotNull(resolver);
            var initialEndPointsTask = resolver.GetEndPointsAsync(CancellationToken.None).ConfigureAwait(false);
            sem.Release(1);
            var initialEndPoints = await initialEndPointsTask;
            Assert.NotNull(initialEndPoints);
            Assert.Single(initialEndPoints);

            // Tell the resolver to throw on the next resolve call and then trigger a reload.
            throwOnNextResolve[0] = true;
            cts[0].Cancel();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var resolveTask = resolver.GetEndPointsAsync(CancellationToken.None);
                sem.Release(1);
                await resolveTask.ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.Equal("throwing", exception.Message);

            var channel = Channel.CreateUnbounded<ServiceEndPointResolverResult>();
            resolver.OnEndPointsUpdated = result => channel.Writer.TryWrite(result);

            do
            {
                cts[0].Cancel();
                sem.Release(1);
                var resolveTask = resolver.GetEndPointsAsync(CancellationToken.None);
                await resolveTask.ConfigureAwait(false);
                var next = await channel.Reader.ReadAsync(CancellationToken.None).ConfigureAwait(false);
                if (next.ResolvedSuccessfully)
                {
                    break;
                }
            } while (true);

            var task = resolver.GetEndPointsAsync(CancellationToken.None);
            sem.Release(1);
            var endPoints = await task.ConfigureAwait(false);
            Assert.NotSame(initialEndPoints, endPoints);
            var sep = Assert.Single(endPoints);
            var ip = Assert.IsType<IPEndPoint>(sep.EndPoint);
            Assert.Equal(IPAddress.Parse("127.1.1.1"), ip.Address);
            Assert.Equal(8080, ip.Port);
        }
    }
}
