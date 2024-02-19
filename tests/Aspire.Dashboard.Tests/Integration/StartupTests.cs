// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class StartupTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public StartupTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async void EndPointAccessors_AppStarted_EndPointPortsAssigned()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);

        // Act
        await app.StartAsync();

        // Assert
        AssertDynamicIPEndpoint(app.BrowserEndPointAccessor);
        AssertDynamicIPEndpoint(app.OtlpServiceEndPointAccessor);
    }

    private static void AssertDynamicIPEndpoint(Func<IPEndPoint> endPointAccessor)
    {
        // Check that the specified dynamic port of 0 is overridden with the actual port number.
        var ipEndPoint = endPointAccessor();
        Assert.NotEqual(0, ipEndPoint.Port);
    }
}
