// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class TargetLocationInterceptorTests
{
    [Fact]
    public void InterceptTargetLocation_RelativeRoot_Redirect()
    {
        Assert.True(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "/", out var newTargetLocation));
        Assert.Equal(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [Fact]
    public void InterceptTargetLocation_Absolute_Redirect()
    {
        Assert.True(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "http://localhost/", out var newTargetLocation));
        Assert.Equal(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [Fact]
    public void InterceptTargetLocation_Absolute_WithoutTrailingSlash_Redirect()
    {
        Assert.True(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "http://localhost", out var newTargetLocation));
        Assert.Equal(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [Fact]
    public void InterceptTargetLocation_AbsoluteDifferentCase_Redirect()
    {
        Assert.True(TargetLocationInterceptor.InterceptTargetLocation("http://LOCALHOST", "http://localhost/", out var newTargetLocation));
        Assert.Equal(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [Fact]
    public void InterceptTargetLocation_StructuredLogs_Unchanged()
    {
        Assert.False(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", TargetLocationInterceptor.StructuredLogsPath, out _));
    }

    [Fact]
    public void InterceptTargetLocation_DifferentHost_Unchanged()
    {
        Assert.False(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "http://localhost:8888/", out _));
    }

    [Fact]
    public void InterceptTargetLocation_DifferentHost_TrailingSlash_Unchanged()
    {
        Assert.False(TargetLocationInterceptor.InterceptTargetLocation("http://localhost/", "http://localhost:8888/", out _));
    }
}
