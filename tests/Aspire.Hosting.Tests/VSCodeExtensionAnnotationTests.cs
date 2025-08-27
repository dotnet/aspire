// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class VSCodeExtensionAnnotationTests
{
    [Fact]
    public void CanCreateVSCodeExtensionAnnotation()
    {
        var annotation = new VSCodeExtensionAnnotation("ms-python.python");

        Assert.Equal("ms-python.python", annotation.Id);
    }

    [Fact]
    public void VSCodeExtensionAnnotationRequiredParameters()
    {
        Assert.Throws<ArgumentException>(() => new VSCodeExtensionAnnotation(""));
    }

    [Fact]
    public void WithVSCodeExtensionRecommendationAddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("container", "image")
                               .WithVSCodeExtensionRecommendation("ms-python.python");

        var annotation = container.Resource.Annotations.OfType<VSCodeExtensionAnnotation>().Single();

        Assert.Equal("ms-python.python", annotation.Id);
    }
}