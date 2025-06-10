// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public abstract class LoopbackDnsTestBase : IDisposable
{
    protected readonly ITestOutputHelper Output;

    internal LoopbackDnsServer DnsServer { get; }
    private readonly Lazy<DnsResolver> _resolverLazy;
    internal DnsResolver Resolver => _resolverLazy.Value;
    internal ResolverOptions Options { get; }
    protected readonly FakeTimeProvider TimeProvider;

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetTimeProvider")]
    static extern void MockTimeProvider(DnsResolver instance, TimeProvider provider);

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
        var resolver = new DnsResolver(Options);
        MockTimeProvider(resolver, TimeProvider);
        return resolver;
    }

    public void Dispose()
    {
        DnsServer.Dispose();
    }
}
