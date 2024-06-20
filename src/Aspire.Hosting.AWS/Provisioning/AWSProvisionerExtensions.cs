// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.AWS.Provisioning;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding support for generating AWS resources dynamically during application startup.
/// </summary>
public static class AWSProvisionerExtensions
{
    /// <summary>
    /// Adds support for generating azure resources dynamically during application startup.
    /// The application must configure the appropriate subscription, location.
    /// </summary>
    public static IDistributedApplicationBuilder AddAWSProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<AWSProvisioner>();
        builder.AddAWSProvisioner<CDKResource, CDKStackResourceProvisioner<CDKResource>>();
        builder.AddAWSProvisioner<StackResource, CDKStackResourceProvisioner<StackResource>>();;
        builder.AddAWSProvisioner<CloudFormationStackResource, CloudFormationStackResourceProvisioner>();
        builder.AddAWSProvisioner<CloudFormationTemplateResource, CloudFormationTemplateResourceProvisioner>();
        return builder;
    }

    internal static IDistributedApplicationBuilder AddAWSProvisioner<TResource, TProvisioner>(this IDistributedApplicationBuilder builder)
        where TResource : IAWSResource
        where TProvisioner : AWSResourceProvisioner<TResource>
    {
        builder.Services.AddKeyedSingleton<IAWSResourceProvisioner, TProvisioner>(typeof(TResource));
        return builder;
    }
}
