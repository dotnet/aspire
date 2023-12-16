// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.CloudFormation;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

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
    public static IResourceBuilder<ICloudFormationResource> AddAWSCloudFormationProvisioning(this IDistributedApplicationBuilder builder, string stackName, string templatePath)
    {
        var resource = new CloudFormationResource(stackName, templatePath);
        var cfBuilder = builder.AddResource(resource);

        builder.Services.AddLifecycleHook<CloudFormationLifeCycle>(sp => ActivatorUtilities.CreateInstance<CloudFormationLifeCycle>(sp, resource));
        return cfBuilder;
    }

    /// <summary>
    /// The AWS credential profile to use for resolving credentials to make AWS service API calls.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="profile">The name of the AWS credential profile.</param>
    public static IResourceBuilder<ICloudFormationResource> WithAWSProfile(this IResourceBuilder<ICloudFormationResource> builder, string profile)
    {
        builder.Resource.Profile = profile;
        return builder;
    }

    /// <summary>
    /// The AWS region to deploy the CloudFormation Stack.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="region">The AWS region to send service requests to.</param>
    public static IResourceBuilder<ICloudFormationResource> WithAWSRegion(this IResourceBuilder<ICloudFormationResource> builder, RegionEndpoint region)
    {
        builder.Resource.Region = region;
        return builder;
    }

    /// <summary>
    /// The configured Amazon CloudFormation service client used to make service calls. If the service client is provided
    /// then the value for WithProfile and WithRegion are ignored.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cloudFormationClient">The AWS CloudFormation service client.</param>
    public static IResourceBuilder<ICloudFormationResource> WithAWSCloudFormationClient(this IResourceBuilder<ICloudFormationResource> builder, IAmazonCloudFormation cloudFormationClient)
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
    public static IResourceBuilder<TDestination> WithAWSCloudFormationReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<ICloudFormationResource> cloudFormationResourceBuilder, string configSection = "AWS::Resources")
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
