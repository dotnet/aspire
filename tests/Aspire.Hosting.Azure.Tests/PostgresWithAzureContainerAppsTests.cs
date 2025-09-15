// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class PostgresWithAzureContainerAppsTests
{
    [Fact]
    public async Task PostgresWithDataVolumeHasCorrectMountOptions()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddAzureContainerAppEnvironment("env");

        var postgres = builder.AddPostgres("postgres")
            .WithDataVolume()
            .PublishAsAzureContainerApp((infra, app) => { });

        using var app = builder.Build();

        // Only execute the BeforeStartAsync hooks, not the full application startup
        var lifecycleHooks = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        var beforeStartHooks = lifecycleHooks.Where(h => h.GetType().Name == "AzureContainerAppsInfrastructure");
        
        foreach (var hook in beforeStartHooks)
        {
            await hook.BeforeStartAsync(app.Services.GetRequiredService<DistributedApplicationModel>(), CancellationToken.None);
        }

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Get the container app environment and check that it has mount options for Postgres
        var containerAppEnvResource = Assert.Single(model.Resources.OfType<AzureContainerAppEnvironmentResource>());
        var (envManifest, envBicep) = await GetManifestWithBicep(containerAppEnvResource);

        // The mount options should be present in the environment bicep where storage is configured
        // This is now implemented, so the test should pass
        Assert.Contains("uid=999,gid=999", envBicep);
    }
}