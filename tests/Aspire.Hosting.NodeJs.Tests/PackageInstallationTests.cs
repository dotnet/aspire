// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.NodeJs.Tests;

public class PackageInstallationTests
{
    /// <summary>
    /// This test validates that the WithNpm method creates
    /// installer resources with proper arguments and relationships.
    /// </summary>
    [Fact]
    public async Task WithNpm_CanBeConfiguredWithInstallAndCIOptions()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("nodeApp", "./test-app");
        var nodeApp2 = builder.AddNpmApp("nodeApp2", "./test-app-ci");

        // Test that both configurations can be set up without errors
        nodeApp.WithNpm(install: true); // Uses npm install
        nodeApp2.WithNpm(install: false);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var nodeResources = appModel.Resources.OfType<NodeAppResource>().ToList();
        var installerResources = appModel.Resources.OfType<NodeInstallerResource>().ToList();

        Assert.Equal(2, nodeResources.Count);
        Assert.Single(installerResources);
        Assert.All(nodeResources, resource => Assert.Equal("npm", resource.Command));

        var nodeAppInstallResource = installerResources.Single(r => r.Name == "nodeApp-npm-install");
        Assert.Equal("npm", nodeAppInstallResource.Command);
        var args = await nodeAppInstallResource.GetArgumentValuesAsync();
        Assert.Single(args);
        Assert.Equal("install", args[0]);

        var nodeApp2InstallResource = installerResources.SingleOrDefault(r => r.Name == "nodeApp2-npm-install");
        Assert.Null(nodeApp2InstallResource);
    }

    [Fact]
    public void WithNpm_ExcludedFromPublishMode()
    {
        var builder = DistributedApplication.CreateBuilder(["Publishing:Publisher=manifest", "Publishing:OutputPath=./publish"]);

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithNpm(install: true);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal("npm", nodeResource.Command);

        // Verify NO installer resource was created in publish mode
        var installerResources = appModel.Resources.OfType<NodeInstallerResource>().ToList();
        Assert.Empty(installerResources);

        // Verify no wait annotations were added
        Assert.False(nodeResource.TryGetAnnotationsOfType<WaitAnnotation>(out _));
    }
}
