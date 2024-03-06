// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for configuring the AWS SDK for .NET
/// </summary>
public static class SDKResourceExtensions
{
    /// <summary>
    /// Add a configuration for resolving region and credentials for the AWS SDK for .NET.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IAWSSDKConfig AddAWSSDKConfig(this IDistributedApplicationBuilder builder)
    {
        var config = new AWSSDKConfig();

        return config;
    }

    /// <summary>
    /// Assign the AWS credential profile to the IAWSSDKConfigResource.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="profile">The name of the AWS credential profile.</param>
    /// <returns></returns>
    public static IAWSSDKConfig WithProfile(this IAWSSDKConfig config, string profile)
    {
        config.Profile = profile;
        return config;
    }

    /// <summary>
    /// Assign the region for the IAWSSDKConfigResource.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="region">The AWS region.</param>
    public static IAWSSDKConfig WithRegion(this IAWSSDKConfig config, RegionEndpoint region)
    {
        config.Region = region;
        return config;
    }

    /// <summary>
    /// Add a reference to an AWS SDK configuration a project.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="awsSdkConfig">The AWS SDK configuration</param>
    /// <returns></returns>
    public static IResourceBuilder<ProjectResource> WithReference(this IResourceBuilder<ProjectResource> builder, IAWSSDKConfig awsSdkConfig)
    {
        builder.WithEnvironment(context =>
        {
            if (context.ExecutionContext.IsPublishMode)
            {
                return;
            }

            if (!string.IsNullOrEmpty(awsSdkConfig.Profile))
            {
                // The environment variable that AWSSDK.Extensions.NETCore.Setup will look for via IConfiguration.
                context.EnvironmentVariables["AWS__Profile"] = awsSdkConfig.Profile;

                // The environment variable the service clients look for service clients created without AWSSDK.Extensions.NETCore.Setup.
                context.EnvironmentVariables["AWS_PROFILE"] = awsSdkConfig.Profile;
            }

            if (awsSdkConfig.Region != null)
            {
                // The environment variable that AWSSDK.Extensions.NETCore.Setup will look for via IConfiguration.
                context.EnvironmentVariables["AWS__Region"] = awsSdkConfig.Region.SystemName;

                // The environment variable the service clients look for service clients created without AWSSDK.Extensions.NETCore.Setup.
                context.EnvironmentVariables["AWS_REGION"] = awsSdkConfig.Region.SystemName;
            }
        });

        return builder;
    }
}
