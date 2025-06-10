// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Yarp.ReverseProxy.Configuration;

namespace Aspire.Hosting.Yarp.Tests;
public class YarpFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9344")]
    public async Task VerifyYarpResourceConfigFile()
    {
        await VerifyYarpResource(true);
    }

    [Fact]
    [RequiresDocker]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/9344")]
    public async Task VerifyYarpResourceProgrammaticConfig()
    {
        await VerifyYarpResource(false);
    }

    private async Task VerifyYarpResource(bool useProgrammaticConfig)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var backend = builder
            .AddContainer("backend", "mcr.microsoft.com/dotnet/samples:aspnetapp")
            .WithHttpEndpoint(targetPort: 8080)
            .WithExternalHttpEndpoints();

        var yarp = builder.AddYarp("yarp");
        if (useProgrammaticConfig)
        {
            yarp.Configure(configuration =>
            {
                configuration
                    .AddRoute(new RouteConfig()
                    {
                        RouteId = "route1",
                        ClusterId = "cluster1",
                        Match = new RouteMatch()
                        {
                            Path = "/aspnetapp/{**catch-all}"
                        },
                        Transforms = new[]
                        {
                            new Dictionary<string, string>
                            {
                                { "PathRemovePrefix", "/aspnetapp" },
                            }
                        }
                    })
                    .AddCluster(new ClusterConfig()
                    {
                        ClusterId = "cluster1",
                        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "destination1", new DestinationConfig { Address = "http://backend" } },
                        }
                    });
            });
        }
        else
        {
            yarp.WithConfigFile("yarp.json");
        }

        yarp.WithReference(backend.GetEndpoint("http"))
            .WithHttpHealthCheck("/heath", 404); // TODO we don't have real health check path yet

        var app = builder.Build();

        await app.StartAsync();

        await app.ResourceNotifications.WaitForResourceHealthyAsync(yarp.Resource.Name, cts.Token);

        var endpoint = yarp.Resource.GetEndpoint("http");
        var httpClient = new HttpClient() { BaseAddress = new Uri(endpoint.Url) };

        using var response200 = await httpClient.GetAsync("/aspnetapp");
        Assert.Equal(System.Net.HttpStatusCode.OK, response200.StatusCode);

        using var response404 = await httpClient.GetAsync("/another");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response404.StatusCode);

        await app.StopAsync();
    }
}
