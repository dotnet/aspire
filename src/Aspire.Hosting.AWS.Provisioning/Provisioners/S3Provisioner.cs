// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation.Constructs;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.AWS.Provisioning.Provisioners;

internal sealed class S3Provisioner : AwsResourceProvisioner<AwsS3BucketResource, AwsS3BucketConstruct>
{
    public override void ConfigureResource(IConfiguration configuration, AwsS3BucketResource resource)
    {
        var bucketSection = configuration.GetSection($"AWS:S3:{resource.Name}");
        var bucketName = bucketSection["BucketName"];

        resource.BucketName = bucketName;

        // TODO: add tags, versioning, or other S3-specific settings
    }

    public override AwsS3BucketConstruct CreateConstruct(AwsS3BucketResource resource, ProvisioningContext context)
    {
        var bucketConstruct = new AwsS3BucketConstruct(resource.Name)
        {
            Properties = new AwsS3BucketConstruct.BucketProperties
            {
                BucketName = resource.BucketName
                // Additional properties can be set here based on the resource definition
            }
        };

        return bucketConstruct;
    }
}
