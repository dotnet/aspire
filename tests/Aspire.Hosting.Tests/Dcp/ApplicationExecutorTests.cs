// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests.Dcp;

public class ApplicationExecutorTests
{
    [Fact]
    public async Task ContainersArePassedOtelServiceName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("CustomName", "container").WithOtlpExporter();

        var kubernetesService = new MockKubernetesService();

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);

        // Act
        await appExecutor.RunApplicationAsync();

        // Assert
        var container = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.Equal("CustomName", container.Metadata.Annotations["otel-service-name"]);
    }

    private static ApplicationExecutor CreateAppExecutor(
        DistributedApplicationModel distributedAppModel,
        IConfiguration? configuration = null,
        IKubernetesService? kubernetesService = null)
    {
        if (configuration == null)
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost",
                ["AppHost:BrowserToken"] = "TestBrowserToken!",
                ["AppHost:OtlpApiKey"] = "TestOtlpApiKey!"
            });

            configuration = builder.Build();
        }
        return new ApplicationExecutor(
            NullLogger<ApplicationExecutor>.Instance,
            NullLogger<DistributedApplication>.Instance,
            distributedAppModel,
            kubernetesService ?? new MockKubernetesService(),
            Array.Empty<IDistributedApplicationLifecycleHook>(),
            configuration,
            Options.Create(new DcpOptions
            {
                DashboardPath = "./dashboard"
            }),
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            new ResourceNotificationService(new NullLogger<ResourceNotificationService>()),
            new ResourceLoggerService(),
            new TestDcpDependencyCheckService()
            );
    }
}
