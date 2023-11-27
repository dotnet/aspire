// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
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
        var accessControl = bucketSection["AccessControl"];

        resource.BucketName = bucketName;
        resource.AccessControl = accessControl;

        // TODO: add tags, versioning, or other S3-specific settings
    }

    public override AwsS3BucketConstruct CreateConstruct(AwsS3BucketResource resource, ProvisioningContext context)
    {
        var bucketConstruct = new AwsS3BucketConstruct(resource.Name)
        {
            Properties = new AwsS3BucketConstruct.BucketProperties
            {
                BucketName = resource.BucketName,
                AccessControl = resource.AccessControl
                // Additional properties can be set here based on the resource definition
            }
        };

        return bucketConstruct;
    }

    public override void SetResourceOutputs(AwsS3BucketResource awsResource, IImmutableDictionary<string, string> resourceOutputs)
    {
        awsResource.Arn = resourceOutputs[$"{awsResource.Name}-BucketArn"];
        awsResource.BucketName = resourceOutputs[$"{awsResource.Name}-BucketName"];
    }
}
