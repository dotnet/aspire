// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Containers.Tests;

public class ContainerLabelAnnotationTests
{
    [Fact]
    public async Task WithLabelAddsLabelAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("com.example.service", "my-service");

        var annotation = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().Single();
        
        // Test by executing the callback
        var labels = new Dictionary<string, string>();
        var context = new ContainerLabelCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), container.Resource, labels, CancellationToken.None);
        await annotation.Callback(context);
        
        Assert.Single(labels);
        Assert.Equal("my-service", labels["com.example.service"]);
    }

    [Fact]
    public async Task WithLabelAddsMultipleLabelsToSeparateAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("com.example.service", "my-service")
                               .WithLabel("com.example.environment", "staging");

        var annotations = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().ToArray();
        Assert.Equal(2, annotations.Length);
        
        // Test by executing the callbacks
        var labels = new Dictionary<string, string>();
        var context = new ContainerLabelCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), container.Resource, labels, CancellationToken.None);
        
        foreach (var annotation in annotations)
        {
            await annotation.Callback(context);
        }
        
        Assert.Equal(2, labels.Count);
        Assert.Equal("my-service", labels["com.example.service"]);
        Assert.Equal("staging", labels["com.example.environment"]);
    }

    [Fact]
    public async Task WithLabelsAddsDictionaryOfLabels()
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

        var annotation = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().Single();
        
        // Test by executing the callback
        var resultLabels = new Dictionary<string, string>();
        var context = new ContainerLabelCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), container.Resource, resultLabels, CancellationToken.None);
        await annotation.Callback(context);
        
        Assert.Equal(3, resultLabels.Count);
        Assert.Equal("my-service", resultLabels["com.example.service"]);
        Assert.Equal("staging", resultLabels["com.example.environment"]);
        Assert.Equal("team-xyz", resultLabels["com.example.owner"]);
    }

    [Fact]
    public async Task WithLabelsAndWithLabelCanBeCombined()
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

        var annotations = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().ToArray();
        Assert.Equal(2, annotations.Length);
        
        // Test by executing the callbacks
        var resultLabels = new Dictionary<string, string>();
        var context = new ContainerLabelCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), container.Resource, resultLabels, CancellationToken.None);
        
        foreach (var annotation in annotations)
        {
            await annotation.Callback(context);
        }
        
        Assert.Equal(3, resultLabels.Count);
        Assert.Equal("my-service", resultLabels["com.example.service"]);
        Assert.Equal("staging", resultLabels["com.example.environment"]);
        Assert.Equal("team-xyz", resultLabels["com.example.owner"]);
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
    public async Task WithLabelStaticAndCallbackCanBeCombined()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("static-key", "static-value")
                               .WithLabel("callback-key", () => "callback-value");

        var annotations = container.Resource.Annotations.OfType<ContainerLabelCallbackAnnotation>().ToArray();
        Assert.Equal(2, annotations.Length);
        
        // Test by executing the callbacks
        var resultLabels = new Dictionary<string, string>();
        var context = new ContainerLabelCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), container.Resource, resultLabels, CancellationToken.None);
        
        foreach (var annotation in annotations)
        {
            await annotation.Callback(context);
        }
        
        Assert.Equal(2, resultLabels.Count);
        Assert.Equal("static-value", resultLabels["static-key"]);
        Assert.Equal("callback-value", resultLabels["callback-key"]);
    }
}