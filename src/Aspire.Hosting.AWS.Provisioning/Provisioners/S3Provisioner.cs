// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.AWS.Provisioning.Provisioners;

internal sealed class S3Provisioner(IAmazonS3 amazonS3) : AwsResourceProvisioner<AwsS3BucketResource>
{
    public override void ConfigureResource(IConfiguration configuration, AwsS3BucketResource resource)
    {

        var bucketSection = configuration.GetSection($"AWS:S3:{resource.Name}");
        var bucketName = bucketSection["BucketName"];
        // Validate and get the region name;
        var region = bucketSection["Region"];
        var regionEndpoint = region != null ? RegionEndpoint.GetBySystemName(region) : RegionEndpoint.USEast1;

        if (bucketName is null)
        {
            // TODO: Handle
        }

        resource.BucketName = bucketName;
        resource.S3Region = regionEndpoint.SystemName;
    }

    public override async Task GetOrCreateResourceAsync(AwsS3BucketResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        // TODO: Check if resource exists

        var doesS3BucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(amazonS3, resource.BucketName).ConfigureAwait(false);

        if (doesS3BucketExists)
        {
            return;
        }

        // TOOD: we may need credentials here
        var s3Region = resource.S3Region;

        var putBucketRequest = new PutBucketRequest()
        {
            BucketName = resource.BucketName,
            BucketRegion = s3Region,
        };

        var putButResponse = await amazonS3.PutBucketAsync(putBucketRequest, cancellationToken).ConfigureAwait(false);

        if (putButResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            // TODO: Handle
        }

        // TODO: role assignments
    }
}
