// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public abstract class LoopbackDnsTestBase : IDisposable
{
    protected readonly ITestOutputHelper Output;

    internal LoopbackDnsServer DnsServer { get; }
    internal DnsResolver Resolver { get; }
    protected readonly TestTimeProvider TimeProvider;

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetTimeProvider")]
    static extern void MockTimeProvider(DnsResolver instance, TimeProvider provider);

    public LoopbackDnsTestBase(ITestOutputHelper output)
    {
        Output = output;
        DnsServer = new();
        Resolver = new([DnsServer.DnsEndPoint]);
        Resolver.Timeout = TimeSpan.FromSeconds(5);
        TimeProvider = new();
        MockTimeProvider(Resolver, TimeProvider);
    }

    public void Dispose()
    {
        DnsServer.Dispose();
    }
}
