// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Net;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class ResolvConfTests
{
    [Fact]
    public void GetOptions()
    {
        var contents = @"
nameserver 10.96.0.10
search default.svc.cluster.local svc.cluster.local cluster.local
options ndots:5
@";

        var reader = new StringReader(contents);
        ResolverOptions options = ResolvConf.GetOptions(reader);

        IPEndPoint ipAddress = Assert.Single(options.Servers);
        Assert.Equal(new IPEndPoint(IPAddress.Parse("10.96.0.10"), 53), ipAddress);
    }
}
