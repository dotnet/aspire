// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration;

public class StartupTests
{
    [Fact]
    public async void EndPointAccessors_AppStarted_NotNull()
    {
        // Arrange
        var configBuilder = new ConfigurationManager()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_URLS"] = "http://127.0.0.1:0",
                ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://127.0.0.1:0"
            });

        await using var app = new DashboardWebApplication(configBuilder.Build());

        // Act
        await app.StartAsync();

        // Assert
        AssertDynamicIPEndpoint(app.BrowserEndPointAccessor);
        AssertDynamicIPEndpoint(app.OtlpServiceEndPointAccessor);
    }

    private static void AssertDynamicIPEndpoint(Func<IPEndPoint> endPointAccessor)
    {
        var ipEndPoint = endPointAccessor();
        Assert.NotEqual(0, ipEndPoint.Port);
    }
}
