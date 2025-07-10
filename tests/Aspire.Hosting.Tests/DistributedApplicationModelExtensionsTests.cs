// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationModelExtensionsTests
{
    [Fact]
    public void GetComputeResources_Returns_Containers_Emulators_And_Projects_Excludes_Ignored()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container1 = builder.AddContainer("container1", "image");
        var container2 = builder.AddContainer("container2", "image");
        var project = builder.AddProject<Projects.ServiceA>("ServiceA");
        var emulator = builder.AddResource(new CustomResource() { Annotations = { new EmulatorResourceAnnotation() } });
        var ignored = builder.AddContainer("container3", "image")
            .ExcludeFromManifest();

        var notACompute = builder.AddExecutable("notACompute", "path/to/executable", ".");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var result = appModel.GetComputeResources().ToList();

        Assert.Collection(result,
            item => Assert.Equal(container1.Resource, item),
            item => Assert.Equal(container2.Resource, item),
            item => Assert.Equal(project.Resource, item),
            item => Assert.Equal(emulator.Resource, item));
    }

    private sealed class CustomResource : IResource
    {
        public string Name { get; set; } = "CustomResource";

        public ResourceAnnotationCollection Annotations { get; } = [];
    }
}
