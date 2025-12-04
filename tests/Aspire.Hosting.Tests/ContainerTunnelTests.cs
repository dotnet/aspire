// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Yarp.Transforms;
using Aspire.TestUtilities;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Hosting.Tests;

public class ContainerTunnelTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task ContainerTunnelWorksWithYarp()
    {
        const string testName = "container-tunnel-works-with-yarp";
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        builder.Configuration[KnownConfigNames.EnableContainerTunnel] = "true";

        var servicea = builder.AddProject<Projects.ServiceA>($"{testName}-servicea");

        var yarp = builder.AddYarp(testName).WithConfiguration(conf =>
        {
            conf.AddRoute("/servicea/{**catch-all}", servicea).WithTransformPathRemovePrefix("/servicea");
        });

        using var app = builder.Build();
        await app.StartAsync().DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        await app.WaitForTextAsync("Application started.").DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);

        using var clientA = app.CreateHttpClient(yarp.Resource.Name, "http");
        var response = await clientA.GetAsync("/servicea/").DefaultTimeout(TestConstants.DefaultOrchestratorTestTimeout);
        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync().DefaultTimeout(TestConstants.DefaultOrchestratorTestTimeout);
        Assert.Equal("Hello World!", body);

        await app.StopAsync().DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
    }
}
