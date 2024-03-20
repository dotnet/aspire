// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

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

    private sealed class TestResource(string name, string connectionString) : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }
}
