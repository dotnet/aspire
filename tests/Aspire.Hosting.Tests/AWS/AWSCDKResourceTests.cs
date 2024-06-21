// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.CDK.AWS.S3;
using Aspire.Hosting.Utils;
using Constructs;
using Xunit;

namespace Aspire.Hosting.Tests.AWS;

public class AWSCDKResourceTests
{
    [Fact]
    public void AddAWSCDKResourceTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var awsSdkConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest1)
            .WithProfile("test-profile");

        var resource = builder.AddAWSCDK("CDK")
            .WithReference(awsSdkConfig)
            .Resource;

        Assert.Equal("CDK", resource.Name);
        Assert.NotNull(resource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest1, resource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", resource.AWSSDKConfig.Profile);
    }

    [Fact]
    public void AddAWSCDKResourceWithAdditionalStackTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var awsSdkConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest1)
            .WithProfile("test-profile");

        var cdk = builder.AddAWSCDK("CDK")
            .WithReference(awsSdkConfig);
        var resource = cdk.AddStack("Stack").Resource;

        Assert.Equal("Stack", resource.Name);
        Assert.NotNull(resource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest1, resource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", resource.AWSSDKConfig.Profile);
    }

    [Fact]
    public void AddAWSCDKResourceWithAdditionalStackAndConfigTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var awsSdkConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest1)
            .WithProfile("test-profile");
        var awsSdkStackConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest2)
            .WithProfile("other-test-profile");

        var cdk = builder.AddAWSCDK("CDK")
            .WithReference(awsSdkConfig);
        var cdkResource = cdk.Resource;
        var stackResource = cdk.AddStack("Stack").WithReference(awsSdkStackConfig).Resource;

        // Assert Stack resource
        Assert.Equal("Stack", stackResource.Name);
        Assert.NotNull(stackResource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest2, stackResource.AWSSDKConfig.Region);
        Assert.Equal("other-test-profile", stackResource.AWSSDKConfig.Profile);

        // Assert CDK resource
        Assert.Equal("CDK", cdkResource.Name);
        Assert.NotNull(cdkResource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest1, cdkResource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", cdkResource.AWSSDKConfig.Profile);
    }

    [Fact]
    public void AddAWSCDKResourceWithConstructTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cdk = builder.AddAWSCDK("CDK");
        var resource = cdk.AddConstruct("Construct", scope => new Construct(scope, "construct")).Resource;

        Assert.Equal("Construct", resource.Name);
        Assert.Equal(cdk.Resource, resource.Parent);
    }

    [Fact]
    public async Task ManifestAWSCDKResourceTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cdk = builder.AddAWSCDK("cdk");
        var resourceBuilder = cdk.AddConstruct("construct", scope => new Bucket(scope, "bucket"));

        builder.AddProject<Projects.ServiceA>("serviceA")
            .WithReference(resourceBuilder, bucket => bucket.BucketName, "BucketName");

        var resource = cdk.Resource;
        Assert.NotNull(resource);

        var expectedManifest = """
       {
         "type": "aws.cloudformation.stack.v0",
         "stack-name": "Aspire-cdk",
         "references": [
           {
             "target-resource": "serviceA"
           }
         ]
       }
       """;

        var manifest = await ManifestUtils.GetManifest(resource);
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task ManifestAWSCDKResourceWithConstructTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cdk = builder.AddAWSCDK("CDK");
        var resourceBuilder = cdk.AddConstruct("Construct", scope => new Bucket(scope, "bucket"));

        builder.AddProject<Projects.ServiceA>("serviceA")
            .WithReference(resourceBuilder, bucket => bucket.BucketName, "BucketName");

        var resource = resourceBuilder.Resource;
        Assert.NotNull(resource);

        var expectedManifest = """
       {
         "type": "aws.cdk.construct.v0",
         "construct-name": "Construct",
         "stack-unique-id": "bucket071C9492",
         "references": [
           {
             "parent-resource": "CDK"
           },
           {
             "target-resource": "serviceA",
             "output-name": "BucketName"
           }
         ]
       }
       """;

        var manifest = await ManifestUtils.GetManifest(resource);
        Assert.Equal(expectedManifest, manifest.ToString());
    }

}
