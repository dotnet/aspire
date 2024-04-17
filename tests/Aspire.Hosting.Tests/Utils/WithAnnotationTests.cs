// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Utils;

public class WithAnnotationTests
{
    [Fact]
    public void WithAnnotationNoExplicitBehaviorAppends()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis")
                           .WithAnnotation(new DummyAnnotation())
                           .WithAnnotation(new DummyAnnotation());

        var dummyAnnotations = redis.Resource.Annotations.OfType<DummyAnnotation>();

        Assert.Equal(2, dummyAnnotations.Count());
        Assert.NotEqual(dummyAnnotations.First(), dummyAnnotations.Last());
    }

    [Fact]
    public void WithAnnotationAddReplaceBehaviorReplaces()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis").WithAnnotation(new DummyAnnotation());

        var firstAnnotation = redis.Resource.Annotations.OfType<DummyAnnotation>().Single();

        redis.WithAnnotation(new DummyAnnotation(), ResourceAnnotationMutationBehavior.Replace);

        var secondAnnotation = redis.Resource.Annotations.OfType<DummyAnnotation>().Single();

        Assert.NotEqual(firstAnnotation, secondAnnotation);
    }
}

public class DummyAnnotation : IResourceAnnotation
{
}
