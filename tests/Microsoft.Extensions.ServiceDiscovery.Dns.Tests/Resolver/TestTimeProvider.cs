// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class TestTimeProvider : TimeProvider
{
    public DateTime Now { get; set; } = DateTime.UtcNow;
    public void Advance(TimeSpan time) => Now += time;

    public override DateTimeOffset GetUtcNow() => Now;
}
