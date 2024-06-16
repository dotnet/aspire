// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Containers.Tests;

public class ContainerResourceTests
{
    [Fact]
    public void AddContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddContainer("container", "none");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("container", containerResource.Name);
        var containerAnnotation = Assert.IsType<ContainerImageAnnotation>(Assert.Single(containerResource.Annotations));
        Assert.Equal("latest", containerAnnotation.Tag);
        Assert.Equal("none", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public void AddContainerAddsAnnotationMetadataWithTag()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddContainer("container", "none", "nightly");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.Single(containerResources);
        Assert.Equal("container", containerResource.Name);
        var containerAnnotation = Assert.IsType<ContainerImageAnnotation>(Assert.Single(containerResource.Annotations));
        Assert.Equal("nightly", containerAnnotation.Tag);
        Assert.Equal("none", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);
    }

    [Fact]
    public async Task AddContainerWithArgs()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var testResource = new TestResource("test", "connectionString");

        var c1 = appBuilder.AddContainer("c1", "image2")
            .WithEndpoint("ep", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 1234);
            });

        var c2 = appBuilder.AddContainer("container", "none")
             .WithArgs(context =>
             {
                 context.Args.Add("arg1");
                 context.Args.Add(c1.GetEndpoint("ep"));
                 context.Args.Add(testResource);
             });

        using var app = appBuilder.Build();

        var args = await ArgumentEvaluator.GetArgumentListAsync(c2.Resource);

        Assert.Collection(args,
            arg => Assert.Equal("arg1", arg),
            arg => Assert.Equal("http://localhost:1234", arg),
            arg => Assert.Equal("connectionString", arg));

        var manifest = await ManifestUtils.GetManifest(c2.Resource);

        var expectedManifest =
        """
        {
          "type": "container.v0",
          "image": "none:latest",
          "args": [
            "arg1",
            "{c1.bindings.ep.url}",
            "{test.connectionString}"
          ]
        }
        """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task EnsureContainerWithEndpointsEmitsContainerPort()
    {
        var builder = DistributedApplication.CreateBuilder(["--publisher", "manifest"]);

        builder.AddContainer("grafana", "grafana/grafana")
               .WithHttpEndpoint(3000);

        using var app = builder.Build();

        var containerResource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().GetContainerResources());

        var manifest = await ManifestUtils.GetManifest(containerResource);

        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "grafana/grafana:latest",
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 3000
                }
              }
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task EnsureContainerWithCustomEntrypointEmitsEntrypoint()
    {
        var builder = DistributedApplication.CreateBuilder(["--publisher", "manifest"]);

        builder.AddContainer("grafana", "grafana/grafana")
               .WithEntrypoint("custom");

        // Build AppHost so that publisher can be resolved.
        using var app = builder.Build();

        var containerResource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().GetContainerResources());

        var manifest = await ManifestUtils.GetManifest(containerResource);

        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "grafana/grafana:latest",
              "entrypoint": "custom"
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public void AddBindMountResolvesRelativePathsRelativeToTheAppHostDirectory()
    {
        var basePath = OperatingSystem.IsWindows() ? @"C:\root\volumes" : "/root/volumes";

        var appBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { ProjectDirectory = basePath });

        appBuilder.AddContainer("container", "none")
            .WithBindMount("source", "/target");

        using var app = appBuilder.Build();

        var containerResource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().GetContainerResources());

        Assert.True(containerResource.TryGetLastAnnotation<ContainerMountAnnotation>(out var mountAnnotation));

        Assert.Equal(Path.Combine(basePath, "source"), mountAnnotation.Source);
    }

    [Fact]
    public async Task EnsureContainerWithVolumesEmitsVolumes()
    {
        var builder = DistributedApplication.CreateBuilder(["--publisher", "manifest"]);

        builder.AddContainer("containerwithvolumes", "image/name")
            .WithVolume("myvolume", "/mount/here")
            .WithVolume("myreadonlyvolume", "/mount/there", isReadOnly: true)
            .WithVolume(null! /* anonymous volume */, "/mount/everywhere");

        using var app = builder.Build();

        var containerResource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().GetContainerResources());

        var manifest = await ManifestUtils.GetManifest(containerResource);

        var expectedManifest = """
            {
              "type": "container.v0",
              "image": "image/name:latest",
              "volumes": [
                {
                  "name": "myvolume",
                  "target": "/mount/here",
                  "readOnly": false
                },
                {
                  "name": "myreadonlyvolume",
                  "target": "/mount/there",
                  "readOnly": true
                },
                {
                  "target": "/mount/everywhere",
                  "readOnly": false
                }
              ]
            }
            """;

        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task EnsureContainerWithBindMountsEmitsBindMounts()
    {
        var appHostPath = OperatingSystem.IsWindows() ? @"C:\projects\apphost" : "/projects/apphost";

        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            ProjectDirectory = appHostPath,
            Args = ["--publisher", "manifest"]
        });

        builder.AddContainer("containerwithbindmounts", "image/name")
            .WithBindMount("./some/source", "/bound")
            .WithBindMount("not/relative/qualified", "/another/place")
            .WithBindMount(".\\some\\other\\source", "\\mount\\here")
            .WithBindMount("./some/file/path.txt", "/mount/there.txt", isReadOnly: true);

        using var app = builder.Build();

        var containerResource = Assert.Single(app.Services.GetRequiredService<DistributedApplicationModel>().GetContainerResources());

        var manifest = await ManifestUtils.GetManifest(containerResource, manifestDirectory: appHostPath);

        var expectedManifest = """
            {
              "type": "container.v0",
              "image": "image/name:latest",
              "bindMounts": [
                {
                  "source": "some/source",
                  "target": "/bound",
                  "readOnly": false
                },
                {
                  "source": "not/relative/qualified",
                  "target": "/another/place",
                  "readOnly": false
                },
                {
                  "source": "some/other/source",
                  "target": "/mount/here",
                  "readOnly": false
                },
                {
                  "source": "some/file/path.txt",
                  "target": "/mount/there.txt",
                  "readOnly": true
                }
              ]
            }
            """;

        Assert.Equal("containerwithbindmounts", containerResource.Name);
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    private sealed class TestResource(string name, string connectionString) : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }
}
