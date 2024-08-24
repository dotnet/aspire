// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Amazon SNS resources to the application model.
/// </summary>
public static class SNSResourceExtensions
{

    private const string TopicArnOutputName = "TopicArn";

    /// <summary>
    /// Adds an Amazon SNS topic.
    /// </summary>
    /// <param name="builder">The builder for the AWS CDK stack.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="props">The properties of the topic.</param>
    public static IResourceBuilder<IConstructResource<Topic>> AddSNSTopic(this IResourceBuilder<IStackResource> builder, string name, ITopicProps? props = null)
    {
        return builder.AddConstruct(name, scope => new Topic(scope, name, props));
    }

    /// <summary>Subscribe some endpoint to this topic.</summary>
    /// <param name="builder">The builder for the topic resource.</param>
    /// <param name="destination">The notification destination queue.</param>
    /// <param name="props">>Properties for an SQS subscription.</param>
    public static IResourceBuilder<IConstructResource<Topic>> AddSubscription(this IResourceBuilder<IConstructResource<Topic>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, SqsSubscriptionProps? props = null)
    {
        builder.Resource.Construct.AddSubscription(new SqsSubscription(destination.Resource.Construct, props));
        return builder;
    }

    /// <summary>
    /// Adds a reference of an Amazon SNS topic to a project. The output parameters of the topic are added to the project IConfiguration.
    /// </summary>
    /// <param name="builder">The builder for the resource.</param>
    /// <param name="topic">The Amazon SNS topic resource.</param>
    /// <param name="configSection">The optional config section in IConfiguration to add the output parameters.</param>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Topic>> topic, string? configSection = null)
        where TDestination : IResourceWithEnvironment
    {
        configSection ??= $"{Constants.DefaultConfigSection}:{topic.Resource.Name}";
        var prefix = configSection.ToEnvironmentVariables();
        return builder.WithEnvironment($"{prefix}__{TopicArnOutputName}", topic, t => t.TopicArn, TopicArnOutputName);
    }
}
