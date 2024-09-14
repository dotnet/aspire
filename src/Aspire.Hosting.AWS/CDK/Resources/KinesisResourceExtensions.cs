// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK.AWS.Kinesis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;
using Stream = Amazon.CDK.AWS.Kinesis.Stream;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Amazon SNS resources to the application model.
/// </summary>
public static class KinesisResourceExtensions
{

    private const string StreamArnOutputName = "StreamArn";

    /// <summary>
    /// Adds an Amazon Kinesis stream.
    /// </summary>
    /// <param name="builder">The builder for the AWS CDK stack.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="props">The properties of the stream.</param>
    public static IResourceBuilder<IConstructResource<Stream>> AddKinesisStream(this IResourceBuilder<IStackResource> builder, string name, IStreamProps? props = null)
    {
        return builder.AddConstruct(name, scope => new Stream(scope, name, props));
    }

    /// <summary>
    /// Adds a reference of an Amazon Kinesis stream to a project. The output parameters of the stream are added to the project IConfiguration.
    /// </summary>
    /// <param name="builder">The builder for the resource.</param>
    /// <param name="stream">The Amazon Kinesis stream resource.</param>
    /// <param name="configSection">The optional config section in IConfiguration to add the output parameters.</param>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Stream>> stream, string? configSection = null)
        where TDestination : IResourceWithEnvironment
    {
        configSection ??= $"{Constants.DefaultConfigSection}:{stream.Resource.Name}";
        var prefix = configSection.ToEnvironmentVariables();
        return builder.WithEnvironment($"{prefix}__{StreamArnOutputName}", stream, s => s.StreamArn, StreamArnOutputName);
    }
}
