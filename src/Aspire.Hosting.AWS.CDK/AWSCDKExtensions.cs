// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.Lifecycle;
using Constructs;
using InvalidOperationException = Amazon.CloudFormation.Model.InvalidOperationException;

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
    /// <returns></returns>
    public static IResourceBuilder<IAppResource> AddAWSCDK(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<AWSCDKLifecycleHook>();
        var appResource = builder.Resources.OfType<IAppResource>().SingleOrDefault();
        return appResource is not null ? builder.CreateResourceBuilder(appResource) : builder.AddResource(new AppResource());
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource> AddStack(this IResourceBuilder<IAppResource> builder, string name)
    {
        var app = builder.Resource.App;
        var stack = new Stack(app, name);
        return builder.AddResource(parent => new StackResource(name, stack, parent));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="stackBuilder"></param>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource<T>> AddStack<T>(this IResourceBuilder<IAppResource> builder, string name, StackBuilderDelegate<T> stackBuilder)
        where T : Stack
    {
        var app = builder.Resource.App;
        var stack = stackBuilder(app);
        return builder.AddResource(parent => new StackResource<T>(name, stack, parent));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="constructBuilder"></param>
    /// <returns></returns>
    public static IResourceBuilder<IConstructResource<T>> AddConstruct<T>(this IResourceBuilder<IResourceWithConstruct> builder, string name, ConstructBuilderDelegate<T> constructBuilder)
        where T : Construct
    {
        var construct = constructBuilder(builder.Resource.Construct);
        return builder.AddResource(parent => new ConstructResource<T>(name, construct, builder.Resource));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource> AddAWSCDKStack(this IDistributedApplicationBuilder builder, string name)
        => builder.AddAWSCDK().AddStack(name);

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
        => builder.AddAWSCDK().AddStack(name, stackBuilder);

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="output"></param>
    /// <param name="exportName"></param>
    /// <typeparam name="TStack"></typeparam>
    /// <returns></returns>
    public static IResourceBuilder<IStackResource<TStack>> WithOutput<TStack>(
        this IResourceBuilder<IStackResource<TStack>> builder,
        string name, ConstructOutputDelegate<TStack> output, string? exportName = null)
        where TStack : Stack
    {
        return builder.WithAnnotation(new ConstructOutputAnnotation<TStack>(name, output) { ExportName = exportName});
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="output"></param>
    /// <param name="exportName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IResourceBuilder<IConstructResource> WithOutput<T>(
        this IResourceBuilder<IConstructResource<T>> builder,
        string name, ConstructOutputDelegate<T> output, string? exportName = null)
        where T : Construct
    {
        return builder.WithAnnotation(new ConstructOutputAnnotation<T>(name, output) { ExportName = exportName});
    }

    /// <summary>
    /// Add a reference of a CloudFormations stack to a project. The output parameters of the CloudFormation stack are added to the project IConfiguration.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="builder"></param>
    /// <param name="constructResourceBuilder">The Construct resource.</param>
    /// <param name="configSection">The config section in IConfiguration to add the output parameters.</param>
    /// <returns></returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource> constructResourceBuilder, string configSection = "AWS::Resources")
        where TDestination : IResourceWithEnvironment
    {
        var stackResourceBuilder = constructResourceBuilder.FindResourceBuilder<IStackResource>();
        return stackResourceBuilder is null
            ? throw new InvalidOperationException("No IStackResource found for Construct")
            : builder.WithReference(stackResourceBuilder, configSection);
    }
}
