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
    public void WithLabelOverwritesExistingLabel()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("com.example.service", "my-service")
                               .WithLabel("com.example.service", "updated-service");

        var annotation = container.Resource.Annotations.OfType<ContainerLabelAnnotation>().Single();
        Assert.Single(annotation.Labels);
        Assert.Equal("updated-service", annotation.Labels["com.example.service"]);
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
    public void WithLabelsOverwritesLabelsWithSameKey()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("com.example.service", "original-service")
                               .WithLabels(new Dictionary<string, string>
                               {
                                   ["com.example.service"] = "updated-service",
                                   ["com.example.environment"] = "staging"
                               });

        var annotation = container.Resource.Annotations.OfType<ContainerLabelAnnotation>().Single();
        Assert.Equal(2, annotation.Labels.Count);
        Assert.Equal("updated-service", annotation.Labels["com.example.service"]);
        Assert.Equal("staging", annotation.Labels["com.example.environment"]);
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
    public void WithLabelThrowsWhenValueIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage");

        var exception = Assert.Throws<ArgumentNullException>(() => container.WithLabel("key", null!));
        Assert.Equal("value", exception.ParamName);
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
    public void TryGetContainerLabelsReturnsFalseWhenNoLabels()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage");

        var result = container.Resource.TryGetContainerLabels(out var labels);
        Assert.False(result);
        Assert.Null(labels);
    }

    [Fact]
    public void TryGetContainerLabelsReturnsTrueWhenLabelsExist()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithLabel("com.example.service", "my-service");

        var result = container.Resource.TryGetContainerLabels(out var labels);
        Assert.True(result);
        Assert.NotNull(labels);
        Assert.Single(labels);
    }

    [Fact]
    public void ContainerLabelAnnotationIsEnumerable()
    {
        var annotation = new ContainerLabelAnnotation();
        annotation.Add("key1", "value1");
        annotation.Add("key2", "value2");

        var labelList = annotation.ToList();
        Assert.Equal(2, labelList.Count);
        Assert.Contains(new KeyValuePair<string, string>("key1", "value1"), labelList);
        Assert.Contains(new KeyValuePair<string, string>("key2", "value2"), labelList);
    }

    [Fact]
    public void ContainerLabelAnnotationConstructorWithDictionary()
    {
        var initialLabels = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        var annotation = new ContainerLabelAnnotation(initialLabels);
        Assert.Equal(2, annotation.Labels.Count);
        Assert.Equal("value1", annotation.Labels["key1"]);
        Assert.Equal("value2", annotation.Labels["key2"]);
    }

    [Fact]
    public void ContainerLabelAnnotationConstructorThrowsWhenLabelsIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ContainerLabelAnnotation(null!));
        Assert.Equal("labels", exception.ParamName);
    }

    [Fact]
    public void ContainerLabelAnnotationAddThrowsWhenKeyIsNull()
    {
        var annotation = new ContainerLabelAnnotation();
        var exception = Assert.Throws<ArgumentNullException>(() => annotation.Add(null!, "value"));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void ContainerLabelAnnotationAddThrowsWhenValueIsNull()
    {
        var annotation = new ContainerLabelAnnotation();
        var exception = Assert.Throws<ArgumentNullException>(() => annotation.Add("key", null!));
        Assert.Equal("value", exception.ParamName);
    }
}