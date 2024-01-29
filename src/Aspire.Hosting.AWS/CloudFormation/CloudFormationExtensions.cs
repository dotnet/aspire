// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

public static class CloudFormationExtensions
{
    /// <summary>
    /// Add a CloudFormation stack for provisioning application resources.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="stackName">The name of the CloudFormation stack.</param>
    /// <param name="templatePath">The path to the CloudFormation template that defines the CloudFormation stack.</param>
    /// <returns></returns>
    public static IResourceBuilder<ICloudFormationResource> AddAWSCloudFormationTemplate(this IDistributedApplicationBuilder builder, string stackName, string templatePath)
    {
        var resource = new CloudFormationResource(stackName, templatePath);
        var cfBuilder = builder.AddResource(resource);

        builder.Services.TryAddLifecycleHook<CloudFormationLifecycleHook>();
        return cfBuilder;
    }

    /// <summary>
    /// The AWS SDK service client configuration used to create the CloudFormation service client.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="awsSdkConfig">The name of the AWS credential profile.</param>
    public static IResourceBuilder<ICloudFormationResource> WithReference(this IResourceBuilder<ICloudFormationResource> builder, IAWSSDKConfig awsSdkConfig)
    {
        builder.Resource.AWSSDKConfig = awsSdkConfig;
        return builder;
    }

    /// <summary>
    /// Override the CloudFormation service client the ICloudFormationResource would create to interact with the CloudFormation service. This can be used for pointing the
    /// CloudFormation service client to a non-standard CloudFormation endpoint like an emulator.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cloudFormationClient">The AWS CloudFormation service client.</param>
    public static IResourceBuilder<ICloudFormationResource> WithReference(this IResourceBuilder<ICloudFormationResource> builder, IAmazonCloudFormation cloudFormationClient)
    {
        builder.Resource.CloudFormationClient = cloudFormationClient;
        return builder;
    }

    /// <summary>
    /// Add a reference of a CloudFormations stack to a project. The output parameters of the CloudFormation stack are added to the project IConfiguration.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="builder"></param>
    /// <param name="cloudFormationResourceBuilder">The CloudFormation resource.</param>
    /// <param name="configSection">The config section in IConfiguration to add the output parameters.</param>
    /// <returns></returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<ICloudFormationResource> cloudFormationResourceBuilder, string configSection = "AWS::Resources")
        where TDestination : IResourceWithEnvironment
    {
        builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest" || cloudFormationResourceBuilder.Resource.Outputs == null)
            {
                return;
            }

            configSection = configSection.Replace(':', '_');

            foreach(var output in cloudFormationResourceBuilder.Resource.Outputs)
            {
                var envName = $"{configSection}__{output.OutputKey}";
                context.EnvironmentVariables[envName] = output.OutputValue;
            }
        });
        return builder;
    }
}
