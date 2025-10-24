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
    public void WithNpm_CanBeConfiguredWithInstallAndCIOptions()
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

        // Verify the installer exists for nodeApp
        var nodeAppInstallResource = installerResources.Single(r => r.Name == "nodeApp-installer");
        Assert.NotNull(nodeAppInstallResource);

        // Verify no installer for nodeApp2
        var nodeApp2InstallResource = installerResources.SingleOrDefault(r => r.Name == "nodeApp2-installer");
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

    [Fact]
    public void WithYarn_CreatesInstallerWhenInstallIsTrue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithYarn(install: true);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists with yarn command
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal("yarn", nodeResource.Command);

        // Verify the install command annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installAnnotation));
        Assert.Equal("yarn", installAnnotation.Command);
        Assert.Equal(["install"], installAnnotation.Args);

        // Verify the run command annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptRunCommandAnnotation>(out var runAnnotation));
        Assert.Equal(["run"], runAnnotation.Args);

        // Verify the build command annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptBuildCommandAnnotation>(out var buildAnnotation));
        Assert.Equal("yarn", buildAnnotation.Command);
        Assert.Equal(["run", "build"], buildAnnotation.Args);

        // Verify the installer resource was created
        var installerResource = Assert.Single(appModel.Resources.OfType<NodeInstallerResource>());
        Assert.Equal("test-app-installer", installerResource.Name);
    }

    [Fact]
    public void WithYarn_DoesNotCreateInstallerWhenInstallIsFalse()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithYarn(install: false);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists with yarn command
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal("yarn", nodeResource.Command);

        // Verify annotations are set
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var _));
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptRunCommandAnnotation>(out var _));
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptBuildCommandAnnotation>(out var _));

        // Verify NO installer resource was created
        var installerResources = appModel.Resources.OfType<NodeInstallerResource>().ToList();
        Assert.Empty(installerResources);
    }

    [Fact]
    public void WithPnpm_CreatesInstallerWhenInstallIsTrue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithPnpm(install: true);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists with pnpm command
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal("pnpm", nodeResource.Command);

        // Verify the install command annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installAnnotation));
        Assert.Equal("pnpm", installAnnotation.Command);
        Assert.Equal(["install"], installAnnotation.Args);

        // Verify the run command annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptRunCommandAnnotation>(out var runAnnotation));
        Assert.Equal(["run"], runAnnotation.Args);

        // Verify the build command annotation
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptBuildCommandAnnotation>(out var buildAnnotation));
        Assert.Equal("pnpm", buildAnnotation.Command);
        Assert.Equal(["run", "build"], buildAnnotation.Args);

        // Verify the installer resource was created
        var installerResource = Assert.Single(appModel.Resources.OfType<NodeInstallerResource>());
        Assert.Equal("test-app-installer", installerResource.Name);
    }

    [Fact]
    public void WithPnpm_DoesNotCreateInstallerWhenInstallIsFalse()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithPnpm(install: false);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists with pnpm command
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal("pnpm", nodeResource.Command);

        // Verify annotations are set
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var _));
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptRunCommandAnnotation>(out var _));
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptBuildCommandAnnotation>(out var _));

        // Verify NO installer resource was created
        var installerResources = appModel.Resources.OfType<NodeInstallerResource>().ToList();
        Assert.Empty(installerResources);
    }

    [Fact]
    public void WithInstallCommand_CreatesInstallerWithCustomCommand()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithInstallCommand("bun", ["install", "--frozen-lockfile"]);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        // Verify the install command annotation with custom command
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installAnnotation));
        Assert.Equal("bun", installAnnotation.Command);
        Assert.Equal(["install", "--frozen-lockfile"], installAnnotation.Args);

        // Verify the installer resource was created
        var installerResource = Assert.Single(appModel.Resources.OfType<NodeInstallerResource>());
        Assert.Equal("test-app-installer", installerResource.Name);
    }

    [Fact]
    public void WithBuildCommand_SetsCustomBuildCommand()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithBuildCommand("bun", ["run", "build:prod"]);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        // Verify the build command annotation with custom command
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptBuildCommandAnnotation>(out var buildAnnotation));
        Assert.Equal("bun", buildAnnotation.Command);
        Assert.Equal(["run", "build:prod"], buildAnnotation.Args);
    }

    [Fact]
    public void WithInstallCommand_CanOverrideExistingInstallCommand()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithNpm(install: false);
        nodeApp.WithInstallCommand("yarn", ["install", "--production"]);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        // Verify the install command annotation was replaced
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptInstallCommandAnnotation>(out var installAnnotation));
        Assert.Equal("yarn", installAnnotation.Command);
        Assert.Equal(["install", "--production"], installAnnotation.Args);

        // Verify the installer resource was created
        var installerResource = Assert.Single(appModel.Resources.OfType<NodeInstallerResource>());
        Assert.Equal("test-app-installer", installerResource.Name);
    }

    [Fact]
    public void WithBuildCommand_CanOverrideExistingBuildCommand()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");
        nodeApp.WithNpm(install: false);
        nodeApp.WithBuildCommand("pnpm", ["build", "--watch"]);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        // Verify the build command annotation was replaced
        Assert.True(nodeResource.TryGetLastAnnotation<JavaScriptBuildCommandAnnotation>(out var buildAnnotation));
        Assert.Equal("pnpm", buildAnnotation.Command);
        Assert.Equal(["build", "--watch"], buildAnnotation.Args);
    }
}
