// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Tests;
public class CustomResourceLoggerTests
{
    [Fact]
    public async Task AddingResourceLoggerAnnotationAllowsLogging()
    {
        var builder = DistributedApplication.CreateBuilder();

        var testResource = builder.AddResource(new TestResource("myResource"))
            .WithResourceLogger();

        var annotation = testResource.Resource.Annotations.OfType<CustomResourceLoggerAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var enumerator = annotation.WatchAsync().GetAsyncEnumerator();

        var task = enumerator.MoveNextAsync();

        annotation.Logger.LogInformation("Hello, world!");
        annotation.Logger.LogError("Hello, error!");

        await task;

        Assert.Equal("Hello, world!", enumerator.Current[0].Content);
        Assert.False(enumerator.Current[0].IsErrorMessage);

        Assert.Equal("Hello, error!", enumerator.Current[1].Content);
        Assert.True(enumerator.Current[1].IsErrorMessage);

        await enumerator.DisposeAsync();

        var backlogEnumerator = annotation.WatchAsync().GetAsyncEnumerator();

        await backlogEnumerator.MoveNextAsync();

        Assert.Equal("Hello, world!", backlogEnumerator.Current[0].Content);
        Assert.Equal("Hello, error!", backlogEnumerator.Current[1].Content);

        await backlogEnumerator.DisposeAsync();
    }

    private sealed class TestResource(string name) : Resource(name)
    {

    }
}
