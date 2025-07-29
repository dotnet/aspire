// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Containers.Tests;

public class ContainerLabelAnnotationTests
{
    [Fact]
    public void WithLabelAddsLabelAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("com.example.service", "my-service");

        var annotation = container.Resource.Annotations.OfType<ContainerLabelAnnotation>().Single();
        Assert.Contains("com.example.service", annotation.Labels.Keys);
        Assert.Equal("my-service", annotation.Labels["com.example.service"]);
    }

    [Fact]
    public void WithLabelAddsMultipleLabelsToSameAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("com.example.service", "my-service")
                               .WithLabel("com.example.environment", "staging");

        var annotation = container.Resource.Annotations.OfType<ContainerLabelAnnotation>().Single();
        Assert.Equal(2, annotation.Labels.Count);
        Assert.Equal("my-service", annotation.Labels["com.example.service"]);
        Assert.Equal("staging", annotation.Labels["com.example.environment"]);
    }

    [Fact]
    public void WithLabelsAddsDictionaryOfLabels()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var labels = new Dictionary<string, string>
        {
            ["com.example.service"] = "my-service",
            ["com.example.environment"] = "staging",
            ["com.example.owner"] = "team-xyz"
        };

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabels(labels);

        var annotation = container.Resource.Annotations.OfType<ContainerLabelAnnotation>().Single();
        Assert.Equal(3, annotation.Labels.Count);
        Assert.Equal("my-service", annotation.Labels["com.example.service"]);
        Assert.Equal("staging", annotation.Labels["com.example.environment"]);
        Assert.Equal("team-xyz", annotation.Labels["com.example.owner"]);
    }

    [Fact]
    public void WithLabelsAndWithLabelCanBeCombined()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var labels = new Dictionary<string, string>
        {
            ["com.example.service"] = "my-service",
            ["com.example.environment"] = "staging"
        };

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabels(labels)
                               .WithLabel("com.example.owner", "team-xyz");

        var annotation = container.Resource.Annotations.OfType<ContainerLabelAnnotation>().Single();
        Assert.Equal(3, annotation.Labels.Count);
        Assert.Equal("my-service", annotation.Labels["com.example.service"]);
        Assert.Equal("staging", annotation.Labels["com.example.environment"]);
        Assert.Equal("team-xyz", annotation.Labels["com.example.owner"]);
    }

    [Fact]
    public void WithLabelThrowsWhenKeyIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage");

        var exception = Assert.Throws<ArgumentNullException>(() => container.WithLabel(null!, "value"));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void WithLabelsThrowsWhenLabelsIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage");

        var exception = Assert.Throws<ArgumentNullException>(() => container.WithLabels(null!));
        Assert.Equal("labels", exception.ParamName);
    }

    [Fact]
    public void WithLabelCallbackCreatesSingleLabelCallbackAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("callback-key", () => "callback-value");

        var annotation = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().Single();
        Assert.NotNull(annotation.Callback);
    }

    [Fact]
    public void WithLabelCallbackActionCreatesMultipleLabelCallbackAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel(context =>
                               {
                                   context.Labels["callback-key1"] = "value1";
                                   context.Labels["callback-key2"] = "value2";
                               });

        var annotation = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().Single();
        Assert.NotNull(annotation.Callback);
    }

    [Fact]
    public void WithLabelCallbackAsyncCreatesMultipleLabelCallbackAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel(async context =>
                               {
                                   context.Labels["async-key1"] = "value1";
                                   context.Labels["async-key2"] = "value2";
                                   await Task.CompletedTask;
                               });

        var annotation = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().Single();
        Assert.NotNull(annotation.Callback);
    }

    [Fact]
    public void WithLabelCallbackThrowsWhenCallbackIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage");

        var exception = Assert.Throws<ArgumentNullException>(() => container.WithLabel("key", (Func<string>)null!));
        Assert.Equal("callback", exception.ParamName);
    }

    [Fact]
    public void WithLabelStaticAndCallbackCanBeCombined()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("static-key", "static-value")
                               .WithLabel("callback-key", () => "callback-value");

        var staticAnnotation = container.Resource.Annotations.OfType<ContainerLabelAnnotation>().Single();
        var callbackAnnotation = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().Single();
        
        Assert.Single(staticAnnotation.Labels);
        Assert.Equal("static-value", staticAnnotation.Labels["static-key"]);
        Assert.NotNull(callbackAnnotation.Callback);
    }
}