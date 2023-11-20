// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.Provisioning;
using Aspire.Hosting.AWS.Provisioning.Provisioners;
using Aspire.Hosting.AWS;
using Aspire.Hosting.Lifecycle;
using LocalStack.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding support for generating AWS resources dynamically during application startup.
/// </summary>
public static class AwsProvisionerExtensions
{
    /// <summary>
    /// Adds support for generating AWS resources dynamically during application startup.
    /// The application must configure the appropriate settings in the <see cref="AwsProvisionerOptions"/> section.
    /// </summary>
    public static IDistributedApplicationBuilder AddAwsProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<AwsProvisioner>();

        builder.Services.AddLocalStack(builder.Configuration);
        builder.Services.AddAwsService<IAmazonCloudFormation>();

        // Attempt to read aws configuration from configuration
        builder.Services.AddOptions<AwsProvisionerOptions>()
            .BindConfiguration("AWS");

        // TODO: We're keeping state in the provisioners, which is not ideal
        builder.Services.AddKeyedTransient<IAwsResourceProvisioner, S3Provisioner>(typeof(AwsS3BucketResource));
        builder.Services.AddKeyedTransient<IAwsResourceProvisioner, SqsProvisioner>(typeof(AwsSqsQueueResource));
        builder.Services.AddKeyedTransient<IAwsResourceProvisioner, SnsProvisioner>(typeof(AwsSnsTopicResource));

        return builder;
    }
}
