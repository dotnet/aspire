// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.NodeJs.Tests;

public class ResourceCreationTests
{
    [Fact]
    public void DefaultViteAppUsesNode()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddViteApp("vite", "vite");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = appModel.Resources.OfType<NodeAppResource>().SingleOrDefault();

        Assert.NotNull(resource);

        Assert.Equal("node", resource.Command);
    }

    [Fact]
    public void ViteAppUsesSpecifiedWorkingDirectory()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddViteApp("vite", "test");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = appModel.Resources.OfType<NodeAppResource>().SingleOrDefault();

        Assert.NotNull(resource);

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "test"), resource.WorkingDirectory);
    }

    [Fact]
    public void ViteAppHasExposedHttpEndpoints()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddViteApp("vite", "vite");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = appModel.Resources.OfType<NodeAppResource>().SingleOrDefault();

        Assert.NotNull(resource);

        Assert.True(resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints));

        Assert.Contains(endpoints, e => e.UriScheme == "http");
    }

    [Fact]
    public void ViteAppHasExposedHttpsEndpoints()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddViteApp("vite", "vite", useHttps: true);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = appModel.Resources.OfType<NodeAppResource>().SingleOrDefault();

        Assert.NotNull(resource);

        Assert.True(resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints));

        Assert.Contains(endpoints, e => e.UriScheme == "https");
    }

    [Fact]
    public void ViteAppDoesNotExposeExternalHttpEndpointsByDefault()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddViteApp("vite", "vite");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = appModel.Resources.OfType<NodeAppResource>().SingleOrDefault();

        Assert.NotNull(resource);

        Assert.True(resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints));

        Assert.DoesNotContain(endpoints, e => e.IsExternal);
    }

    [Fact]
    public async Task WithNpmPackageManagerDefaultsToInstallCommand()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Add package installation with default settings (should use npm install, not ci)
        nodeApp.WithNpmPackageManager(useCI: false);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal("npm", nodeResource.Command);

        // Verify the installer resource was created
        var installerResource = Assert.Single(appModel.Resources.OfType<NpmInstallerResource>());
        Assert.Equal("test-app-npm-install", installerResource.Name);
        Assert.Equal("npm", installerResource.Command);
        var args = await installerResource.GetArgumentValuesAsync();
        Assert.Single(args);
        Assert.Equal("install", args[0]);

        // Verify the parent-child relationship
        Assert.True(installerResource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
        var relationship = Assert.Single(relationships);
        Assert.Same(nodeResource, relationship.Resource);
        Assert.Equal("Parent", relationship.Type);

        // Verify the wait annotation on the parent
        Assert.True(nodeResource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations));
        var waitAnnotation = Assert.Single(waitAnnotations);
        Assert.Same(installerResource, waitAnnotation.Resource);
    }

    [Fact]
    public async Task WithNpmPackageManagerCanUseCICommand()
    {
        var builder = DistributedApplication.CreateBuilder();

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Add package installation with CI enabled
        nodeApp.WithNpmPackageManager(useCI: true);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Verify the NodeApp resource exists
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.Equal("npm", nodeResource.Command);

        // Verify the installer resource was created with CI enabled
        var installerResource = Assert.Single(appModel.Resources.OfType<NpmInstallerResource>());
        Assert.Equal("test-app-npm-install", installerResource.Name);
        Assert.Equal("npm", installerResource.Command);
        var args = await installerResource.GetArgumentValuesAsync();
        Assert.Single(args);
        Assert.Equal("ci", args[0]);

        // Verify the parent-child relationship
        Assert.True(installerResource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships));
        var relationship = Assert.Single(relationships);
        Assert.Same(nodeResource, relationship.Resource);
        Assert.Equal("Parent", relationship.Type);

        // Verify the wait annotation on the parent
        Assert.True(nodeResource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations));
        var waitAnnotation = Assert.Single(waitAnnotations);
        Assert.Same(installerResource, waitAnnotation.Resource);
    }

    [Fact]
    public void ViteAppConfiguresPortFromEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddViteApp("vite", "vite");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());

        // Verify that command line arguments callback is configured
        Assert.True(resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbackAnnotations));
        List<object> args = [];
        var ctx = new CommandLineArgsCallbackContext(args);

        foreach (var annotation in argsCallbackAnnotations)
        {
            annotation.Callback(ctx);
        }

        Assert.Collection(args,
            arg => Assert.Equal("dev", arg),
            arg => Assert.Equal("--port", arg),
            arg => Assert.IsType<EndpointReferenceExpression>(arg)
        );
    }
}
