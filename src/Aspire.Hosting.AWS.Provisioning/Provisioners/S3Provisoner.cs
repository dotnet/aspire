// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.AWS.Provisioning.Provisioners;

internal class S3Provisioner : AwsResourceProvisioner<AwsS3BucketResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AwsS3BucketResource resource)
    {

        var bucketSection = configuration.GetSection($"Azure:S3:{resource.Name}");
        var bucketName = bucketSection["BucketName"];
        var region = bucketSection["Region"];

        // Validate and get the region name;
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        if (bucketName is not null && regionEndpoint is not null)
        {
            resource.BucketName = bucketName;
            resource.S3Region = region;

            return true;
        }
        return false;
    }

    public override async Task GetOrCreateResourceAsync(AwsS3BucketResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        // TODO: Check if resource exists

        // TOOD: we may need credentials here
        var s3Region = resource.S3Region;

        using var client = new AmazonS3Client(new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(s3Region) });

        var putBucketRequest = new PutBucketRequest()
        {
            BucketName = resource.BucketName,
        };

        var putButResponse = await client.PutBucketAsync(putBucketRequest, cancellationToken).ConfigureAwait(false);

        if (putButResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            // TODO: Handle
        }

        // TODO: role assignments
    }
}
