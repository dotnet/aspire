// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public static class IntegrationTestHelpers
{
    public static DashboardWebApplication CreateDashboardWebApplication(ITestOutputHelper testOutputHelper)
    {
        var config = new ConfigurationManager()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_URLS"] = "https://127.0.0.1:0",
                ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://127.0.0.1:0",
                ["Kestrel:Certificates:Default:Path"] = TestCertificateLoader.TestCertificatePath,
                ["Kestrel:Certificates:Default:Password"] = "testPassword"
            }).Build();

        var dashboardWebApplication = new DashboardWebApplication(builder =>
        {
            builder.Configuration.AddConfiguration(config);
            builder.Logging.AddXunit(testOutputHelper);
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
        });

        return dashboardWebApplication;
    }
}
