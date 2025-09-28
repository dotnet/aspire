// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.DevTunnels.Tests;

public class DevTunnelAccessStatusTests
{
    [Fact]
    public void LogAnonymousAccessPolicy_DeniesAnonymousForNoEntries()
    {
        var status = new DevTunnelAccessStatus();
        Assert.Equal("Denied", status.LogAnonymousAccessPolicy(NullLogger.Instance));
    }

    [Fact]
    public void LogAnonymousAccessPolicy_AllowsAnonymousForSingleInheritedAllow()
    {
        var status = new DevTunnelAccessStatus
        {
            AccessControlEntries = [new("Anonymous", IsDeny: false, IsInherited: true, [], ["connect"])]
        };
        Assert.Equal("Allowed", status.LogAnonymousAccessPolicy(NullLogger.Instance));
    }

    [Fact]
    public void LogAnonymousAccessPolicy_AllowsAnonymousForSingleExplicitAllow()
    {
        var status = new DevTunnelAccessStatus
        {
            AccessControlEntries = [new("Anonymous", IsDeny: false, IsInherited: false, [], ["connect"])]
        };
        Assert.Equal("Allowed", status.LogAnonymousAccessPolicy(NullLogger.Instance));
    }

    [Fact]
    public void LogAnonymousAccessPolicy_DeniesAnonymousWhenExplicitlyDeniedWithInheritedAllow()
    {
        var status = new DevTunnelAccessStatus
        {
            AccessControlEntries = [
                new("Anonymous", IsDeny: false, IsInherited: true, [], ["connect"]),
                new("Anonymous", IsDeny: true, IsInherited: false, [], ["connect"])
            ]
        };
        Assert.Equal("Denied", status.LogAnonymousAccessPolicy(NullLogger.Instance));
    }

    [Fact]
    public void LogAnonymousAccessPolicy_DeniesAnonymousForSingleExplicitDeny()
    {
        var status = new DevTunnelAccessStatus
        {
            AccessControlEntries = [
                new("Anonymous", IsDeny: true, IsInherited: false, [], ["connect"])
            ]
        };
        Assert.Equal("Denied", status.LogAnonymousAccessPolicy(NullLogger.Instance));
    }

    [Fact]
    public void LogAnonymousAccessPolicy_AllowsAnonymousWhenExplicitlyAllowedWithInheritedAllowed()
    {
        var status = new DevTunnelAccessStatus
        {
            AccessControlEntries = [
                new("Anonymous", IsDeny: false, IsInherited: true, [], ["connect"]),
                new("Anonymous", IsDeny: false, IsInherited: false, [], ["connect"])
            ]
        };
        Assert.Equal("Allowed", status.LogAnonymousAccessPolicy(NullLogger.Instance));
    }

    [Fact]
    public void LogAnonymousAccessPolicy_ReturnsDeniedForUnexpectedEntries()
    {
        var status = new DevTunnelAccessStatus
        {
            AccessControlEntries = [
                new("Something", IsDeny: false, IsInherited: false, [], ["random"]),
                new("Something", IsDeny: false, IsInherited: false, [], ["random"])
            ]
        };
        Assert.Equal("Denied", status.LogAnonymousAccessPolicy(NullLogger.Instance));
    }
}
