// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;

namespace Aspire.Hosting;

public static class SDKResourceExtensions
{
    /// <summary>
    /// Add a configuration for resolving region and credentials for the AWS SDK for .NET.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IResourceBuilder<IAWSSDKConfigResource> AddAWSSDKConfig(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AWSSDKConfigResource(name);
        var sdkBuilder = builder.AddResource(resource);

        return sdkBuilder;
    }

    /// <summary>
    /// Assign the AWS credential profile to the IAWSSDKConfigResource.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="profile">The name of the AWS credential profile.</param>
    /// <returns></returns>
    public static IResourceBuilder<IAWSSDKConfigResource> WithProfile(this IResourceBuilder<IAWSSDKConfigResource> builder, string profile)
    {
        builder.Resource.Profile = profile;
        return builder;
    }

    /// <summary>
    /// Assign the region for the IAWSSDKConfigResource.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="region">The AWS region.</param>
    public static IResourceBuilder<IAWSSDKConfigResource> WithRegion(this IResourceBuilder<IAWSSDKConfigResource> builder, RegionEndpoint region)
    {
        builder.Resource.Region = region;
        return builder;
    }

    /// <summary>
    /// Add a reference to an AWS SDK configuration a project.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="awsSdkConfig">The AWS SDK configuration</param>
    /// <returns></returns>
    public static IResourceBuilder<ProjectResource> WithAWSSDKReference(this IResourceBuilder<ProjectResource> builder, IResourceBuilder<IAWSSDKConfigResource> awsSdkConfig)
    {
        builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                return;
            }

            if(!string.IsNullOrEmpty(awsSdkConfig.Resource.Profile))
            {
                // The environment variable that AWSSDK.Extensions.NETCore.Setup will look for via IConfiguration.
                context.EnvironmentVariables["AWS__Profile"] = awsSdkConfig.Resource.Profile;

                // The environment variable the service clients look for service clients created without AWSSDK.Extensions.NETCore.Setup.
                context.EnvironmentVariables["AWS_PROFILE"] = awsSdkConfig.Resource.Profile;
            }

            if (awsSdkConfig.Resource.Region != null)
            {
                // The environment variable that AWSSDK.Extensions.NETCore.Setup will look for via IConfiguration.
                context.EnvironmentVariables["AWS__Region"] = awsSdkConfig.Resource.Region.SystemName;

                // The environment variable the service clients look for service clients created without AWSSDK.Extensions.NETCore.Setup.
                context.EnvironmentVariables["AWS_REGION"] = awsSdkConfig.Resource.Region.SystemName;
            }
        });

        return builder;
    }
}
