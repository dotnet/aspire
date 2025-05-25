// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class ObsoleteRedisResourceTests
{
    [Fact]
    public void ObsoleteAzureRedisResourceSupportsNameOutputReference()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        
#pragma warning disable CS0618 // Type or member is obsolete
        // Get the type via reflection to bypass obsolete warnings
        var extensionType = typeof(AzureRedisExtensions);
        var methodInfo = extensionType.GetMethod("AddAzureRedis", new[] { typeof(IDistributedApplicationBuilder), typeof(string) });
        Assert.NotNull(methodInfo);
        
        var redisBuilder = methodInfo.Invoke(null, new object[] { builder, "redis" });
        Assert.NotNull(redisBuilder);
        
        // Get the Resource property
        var resourceProperty = redisBuilder.GetType().GetProperty("Resource");
        Assert.NotNull(resourceProperty);
        
        var resource = resourceProperty.GetValue(redisBuilder);
        Assert.NotNull(resource);
        
        // Get the NameOutputReference property
        var nameOutputReferenceProperty = resource.GetType().GetProperty("NameOutputReference");
        Assert.NotNull(nameOutputReferenceProperty);
        
        var nameOutputReference = nameOutputReferenceProperty.GetValue(resource);
        Assert.NotNull(nameOutputReference);
#pragma warning restore CS0618
    }
}