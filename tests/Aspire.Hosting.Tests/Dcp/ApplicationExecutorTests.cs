// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests.Dcp;

public class ApplicationExecutorTests
{
    [Fact]
    public async Task RunApplicationAsync_NoResources_DashboardStarted()
    {
        // Arrange
        var distributedAppModel = new DistributedApplicationModel(new ResourceCollection());
        var kubernetesService = new MockKubernetesService();

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);

        // Act
        await appExecutor.RunApplicationAsync();

        // Assert
        var dashboard = Assert.IsType<Executable>(Assert.Single(kubernetesService.CreatedResources));
        Assert.Equal("aspire-dashboard", dashboard.Metadata.Name);
    }

    private static ApplicationExecutor CreateAppExecutor(
        DistributedApplicationModel distributedAppModel,
        IConfiguration? configuration = null,
        IKubernetesService? kubernetesService = null)
    {
        return new ApplicationExecutor(
            NullLogger<ApplicationExecutor>.Instance,
            NullLogger<DistributedApplication>.Instance,
            distributedAppModel,
            new DistributedApplicationOptions(),
            kubernetesService ?? new MockKubernetesService(),
            Array.Empty<IDistributedApplicationLifecycleHook>(),
            configuration ?? new ConfigurationBuilder().Build(),
            Options.Create(new DcpOptions
            {
                DashboardPath = "./dashboard"
            }),
            new MockDashboardEndpointProvider(),
            new MockDashboardAvailability());
    }
}
