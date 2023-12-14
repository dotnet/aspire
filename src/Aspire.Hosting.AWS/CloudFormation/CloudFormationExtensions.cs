// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
