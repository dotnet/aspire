// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

/// <summary>
///
/// </summary>
public static class AWSCDKExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="stackBuilder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource<T>> AddAWSCDKStack<T>(this IDistributedApplicationBuilder builder, string name, StackBuilderDelegate<T> stackBuilder)
        where T: Stack
    {
        _ = builder.AddAWSCDKProvisioning();
        return builder.AddResource(new StackResource<T>(name, stackBuilder));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="output"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource> WithOutput<T>(this IResourceBuilder<IStackResource<T>> builder,
        string name, StackOutputDelegate<T> output)
        where T : Stack
    {
        return builder.WithAnnotation(new StackOutputAnnotation<T>(name, output));
    }

    private static IDistributedApplicationBuilder AddAWSCDKProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<AWSCDKLifecycleHook>();
        return builder;
    }
}
