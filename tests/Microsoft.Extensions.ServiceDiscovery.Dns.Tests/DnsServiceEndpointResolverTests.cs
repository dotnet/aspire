// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests;

public class DnsServiceEndpointResolverTests
{
    [Fact]
    public async Task ResolveServiceEndpoint_Dns_MultiShot()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(timeProvider)
            .AddSingleton<IDnsResolver, DnsResolver>()
            .AddServiceDiscoveryCore()
            .AddDnsServiceEndpointProvider(o => o.DefaultRefreshPeriod = TimeSpan.FromSeconds(30))
            .BuildServiceProvider();
        var resolver = services.GetRequiredService<ServiceEndpointResolver>();
        var initialResult = await resolver.GetEndpointsAsync("https://localhost", CancellationToken.None);
        Assert.NotNull(initialResult);
        Assert.True(initialResult.Endpoints.Count > 0);
        timeProvider.Advance(TimeSpan.FromSeconds(7));
        var secondResult = await resolver.GetEndpointsAsync("https://localhost", CancellationToken.None);
        Assert.NotNull(secondResult);
        Assert.True(initialResult.Endpoints.Count > 0);
        timeProvider.Advance(TimeSpan.FromSeconds(80));
        var thirdResult = await resolver.GetEndpointsAsync("https://localhost", CancellationToken.None);
        Assert.NotNull(thirdResult);
        Assert.True(initialResult.Endpoints.Count > 0);
    }
}
