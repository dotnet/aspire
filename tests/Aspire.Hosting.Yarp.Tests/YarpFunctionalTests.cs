// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Yarp.Tests;
public class YarpFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyYarpResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var backend = builder
            .AddContainer("backend", "mcr.microsoft.com/dotnet/samples:aspnetapp")
            .WithHttpEndpoint(targetPort: 8080)
            .WithExternalHttpEndpoints();

        var yarp = builder
            .AddYarp("yarp")
            .WithConfigFile("yarp.json")
            .WithReference(backend.GetEndpoint("http"));

        var app = builder.Build();

        await app.StartAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(yarp.Resource.Name, KnownResourceStates.Running).WaitAsync(cts.Token);

        var endpoint = yarp.Resource.GetEndpoint("http");
        var httpClient = new HttpClient() { BaseAddress = new Uri(endpoint.Url) };
        var errorCounter = 0;

        while (true)
        {
            // We still need to wait a bit for YARP to be really ready
            // TODO: remove when we have healthcheck on YARP container
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            try
            {
                using var response200 = await httpClient.GetAsync("/aspnetapp");
                Assert.Equal(System.Net.HttpStatusCode.OK, response200.StatusCode);

                using var response404 = await httpClient.GetAsync("/another");
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response404.StatusCode);
            }
            catch (HttpRequestException)
            {
                errorCounter++;
                if (errorCounter >= 10)
                {
                    throw;
                }
            }
            break;
        }

        await app.StopAsync();
    }
}
