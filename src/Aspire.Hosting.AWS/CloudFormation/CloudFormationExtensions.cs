// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding AWS CloudFormation as a provisioning resource.
/// </summary>
public static class CloudFormationExtensions
{
    /// <summary>
    /// Add a CloudFormation stack for provisioning application resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="stackName">The name of the CloudFormation stack.</param>
    /// <param name="templatePath">The path to the CloudFormation template that defines the CloudFormation stack.</param>
    /// <returns></returns>
    public static IResourceBuilder<ICloudFormationTemplateResource> AddAWSCloudFormationTemplate(this IDistributedApplicationBuilder builder, string stackName, string templatePath)
    {
        var resource = new CloudFormationTemplateResource(stackName, templatePath);
        var cfBuilder = builder.AddResource(resource)
                                .WithManifestPublishingCallback(resource.WriteToManifest);

        builder.Services.TryAddLifecycleHook<CloudFormationLifecycleHook>();
        return cfBuilder;
    }

    /// <summary>
    /// Add parameters to be provided to CloudFormation when creating the stack for the template.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="parameterName">Name of the CloudFormation parameter.</param>
    /// <param name="parameterValue">Value of the CloudFormation parameter.</param>
    /// <returns></returns>
    public static IResourceBuilder<ICloudFormationTemplateResource> WithParameter(this IResourceBuilder<ICloudFormationTemplateResource> builder, string parameterName, string parameterValue)
    {
        builder.Resource.AddParameter(parameterName, parameterValue);
        return builder;
    }

    /// <summary>
    /// Add a CloudFormation stack for provisioning application resources.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="stackName">The name of the CloudFormation stack.</param>
    /// <returns></returns>
    public static IResourceBuilder<ICloudFormationStackResource> AddAWSCloudFormationStack(this IDistributedApplicationBuilder builder, string stackName)
    {
        var resource = new CloudFormationStackResource(stackName);
        var cfBuilder = builder.AddResource(resource)
                                .WithManifestPublishingCallback(resource.WriteToManifest);

        builder.Services.TryAddLifecycleHook<CloudFormationLifecycleHook>();
        return cfBuilder;
    }

    /// <summary>
    /// Gets a reference to a  output from the CloudFormation stack.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">Name of the output.</param>
    /// <returns>A <see cref="StackOutputReference"/> that represents the output.</returns>
    public static StackOutputReference GetOutput(this IResourceBuilder<ICloudFormationResource> builder, string name)
    {
        return new StackOutputReference(name, builder.Resource);
    }

    /// <summary>
    /// Adds an environment variable to the resource with the value of the output from the CloudFormation stack.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="stackOutputReference">The reference to the CloudFormation stack output.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, StackOutputReference stackOutputReference)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(async ctx =>
        {
            if (ctx.ExecutionContext.IsPublishMode)
            {
                ctx.EnvironmentVariables[name] = stackOutputReference.ValueExpression;
                return;
            }

            if (stackOutputReference.Resource.AWSSDKConfig != null)
            {
                SdkUtilities.ApplySDKConfig(ctx, stackOutputReference.Resource.AWSSDKConfig, false);
            }

            ctx.Logger?.LogInformation("Getting CloudFormation stack output {Name} from resource {ResourceName}", stackOutputReference.Name, stackOutputReference.Resource.Name);

            ctx.EnvironmentVariables[name] = await stackOutputReference.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false) ?? "";
        });
    }

    /// <summary>
    /// The AWS SDK service client configuration used to create the CloudFormation service client.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="awsSdkConfig">The name of the AWS credential profile.</param>
    public static IResourceBuilder<ICloudFormationStackResource> WithReference(this IResourceBuilder<ICloudFormationStackResource> builder, IAWSSDKConfig awsSdkConfig)
    {
        builder.Resource.AWSSDKConfig = awsSdkConfig;
        return builder;
    }

    /// <summary>
    /// The AWS SDK service client configuration used to create the CloudFormation service client.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="awsSdkConfig">The name of the AWS credential profile.</param>
    public static IResourceBuilder<ICloudFormationTemplateResource> WithReference(this IResourceBuilder<ICloudFormationTemplateResource> builder, IAWSSDKConfig awsSdkConfig)
    {
        builder.Resource.AWSSDKConfig = awsSdkConfig;
        return builder;
    }

    /// <summary>
    /// Override the CloudFormation service client the ICloudFormationStackResource would create to interact with the CloudFormation service. This can be used for pointing the
    /// CloudFormation service client to a non-standard CloudFormation endpoint like an emulator.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cloudFormationClient">The AWS CloudFormation service client.</param>
    public static IResourceBuilder<ICloudFormationStackResource> WithReference(this IResourceBuilder<ICloudFormationStackResource> builder, IAmazonCloudFormation cloudFormationClient)
    {
        builder.Resource.CloudFormationClient = cloudFormationClient;
        return builder;
    }

    /// <summary>
    /// Override the CloudFormation service client the ICloudFormationTemplateResource would create to interact with the CloudFormation service. This can be used for pointing the
    /// CloudFormation service client to a non-standard CloudFormation endpoint like an emulator.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cloudFormationClient">The AWS CloudFormation service client.</param>
    public static IResourceBuilder<ICloudFormationTemplateResource> WithReference(this IResourceBuilder<ICloudFormationTemplateResource> builder, IAmazonCloudFormation cloudFormationClient)
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
        cloudFormationResourceBuilder.WithAnnotation(new CloudFormationReferenceAnnotation(builder.Resource.Name));

        builder.WithEnvironment(async ctx =>
        {
            if (ctx.ExecutionContext.IsPublishMode)
            {
                return;
            }

            if (cloudFormationResourceBuilder.Resource.AWSSDKConfig != null)
            {
                SdkUtilities.ApplySDKConfig(ctx, cloudFormationResourceBuilder.Resource.AWSSDKConfig, false);
            }

            if (cloudFormationResourceBuilder.Resource.ProvisioningTaskCompletionSource is not null)
            {
                ctx.Logger?.LogInformation("Waiting on CloudFormation resource {Name} ...", cloudFormationResourceBuilder.Resource.Name);
                await cloudFormationResourceBuilder.Resource.ProvisioningTaskCompletionSource.Task.WaitAsync(ctx.CancellationToken).ConfigureAwait(false);
            }

            if (cloudFormationResourceBuilder.Resource.Outputs == null)
            {
                return;
            }

            configSection = configSection.Replace(':', '_');

            foreach (var output in cloudFormationResourceBuilder.Resource.Outputs)
            {
                var envName = $"{configSection}__{output.OutputKey}";
                ctx.EnvironmentVariables[envName] = output.OutputValue;
            }
        });

        return builder;
    }
}
