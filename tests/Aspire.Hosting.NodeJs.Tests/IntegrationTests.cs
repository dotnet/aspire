// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.NodeJs.Tests;

/// <summary>
/// Integration test that demonstrates the new resource-based package installer architecture.
/// This shows how installer resources appear as separate resources in the application model.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void ResourceBasedPackageInstallersAppearInApplicationModel()
    {
        var builder = DistributedApplication.CreateBuilder();

        // Add a vite app with the npm package manager
        builder.AddViteApp("vite-app", "./frontend")
            .WithNpm(useCI: true);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify all Node.js app resources are present
        var nodeResources = appModel.Resources.OfType<NodeAppResource>().ToList();
        Assert.Single(nodeResources);

        // Verify all installer resources are present as separate resources
        var npmInstallers = appModel.Resources.OfType<NodeInstallerResource>().ToList();

        Assert.Single(npmInstallers);

        // Verify installer resources have expected names (would appear on dashboard)
        Assert.Equal("vite-app-npm-install", npmInstallers[0].Name);

        // Verify parent-child relationships
        foreach (var installer in npmInstallers.Cast<IResource>())
        {
            Assert.True(installer.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
            Assert.Single(relationships);
            Assert.Equal("Parent", relationships.First().Type);
        }

        // Verify all Node.js apps wait for their installers
        foreach (var nodeApp in nodeResources)
        {
            Assert.True(nodeApp.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations));
            Assert.Single(waitAnnotations);

            var waitedResource = waitAnnotations.First().Resource;
            Assert.True(waitedResource is NodeInstallerResource);
        }
    }

    [Fact]
    public void InstallerResourcesHaveCorrectExecutableConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddNpmApp("test-app", "./test")
            .WithNpm(useCI: true);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var installer = Assert.Single(appModel.Resources.OfType<NodeInstallerResource>());

        // Verify it's configured as an ExecutableResource
        Assert.IsAssignableFrom<ExecutableResource>(installer);

        // Verify working directory matches parent
        var parentApp = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal(parentApp.WorkingDirectory, installer.WorkingDirectory);

        // Verify command arguments are configured
        Assert.True(installer.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsAnnotations));
    }
}
