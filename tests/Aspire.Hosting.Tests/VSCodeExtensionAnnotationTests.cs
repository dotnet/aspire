// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class VSCodeExtensionAnnotationTests
{
    [Fact]
    public void CanCreateVSCodeExtensionAnnotation()
    {
        var annotation = new VisualStudioCodeExtensionAnnotation("ms-python.python", "Python", "Python language support");

        Assert.Equal("ms-python.python", annotation.Id);
        Assert.Equal("Python", annotation.DisplayName);
        Assert.Equal("Python language support", annotation.Description);
    }

    [Fact]
    public void VSCodeExtensionAnnotationRequiredParameters()
    {
        Assert.Throws<ArgumentException>(() => new VisualStudioCodeExtensionAnnotation("", "Python"));
        Assert.Throws<ArgumentException>(() => new VisualStudioCodeExtensionAnnotation("ms-python.python", ""));
    }

    [Fact]
    public void WithVSCodeExtensionRecommendationAddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("container", "image")
                               .WithVSCodeExtensionRecommendation("ms-python.python", "Python", "Python language support");

        var annotation = container.Resource.Annotations.OfType<VisualStudioCodeExtensionAnnotation>().Single();

        Assert.Equal("ms-python.python", annotation.Id);
        Assert.Equal("Python", annotation.DisplayName);
        Assert.Equal("Python language support", annotation.Description);
    }
}