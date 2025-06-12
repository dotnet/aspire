// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public abstract class LoopbackDnsTestBase : IDisposable
{
    protected readonly ITestOutputHelper Output;

    internal LoopbackDnsServer DnsServer { get; }
    private readonly Lazy<DnsResolver> _resolverLazy;
    internal DnsResolver Resolver => _resolverLazy.Value;
    internal ResolverOptions Options { get; }
    protected readonly FakeTimeProvider TimeProvider;

    public LoopbackDnsTestBase(ITestOutputHelper output)
    {
        Output = output;
        DnsServer = new();
        TimeProvider = new();
        Options = new([DnsServer.DnsEndPoint])
        {
            Timeout = TimeSpan.FromSeconds(5),
            Attempts = 1,
        };
        _resolverLazy = new(InitializeResolver);
    }

    DnsResolver InitializeResolver()
    {
        ServiceCollection services = new();
        services.AddXunitLogging(Output);

        // construct DnsResolver manually via internal constructor which accepts ResolverOptions
        var resolver = new DnsResolver(TimeProvider, services.BuildServiceProvider().GetRequiredService<ILogger<DnsResolver>>(), Options);
        return resolver;
    }

    public void Dispose()
    {
        DnsServer.Dispose();
    }
}
