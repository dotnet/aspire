// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.CDK.AWS.S3;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Constructs;
using Xunit;

namespace Aspire.Hosting.AWS.Tests;

public class AWSCDKResourceTests
{
    [Fact]
    [RequiresTools(["node"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public void AddAWSCDKResourceTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var awsSdkConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest1)
            .WithProfile("test-profile");

        var resource = builder.AddAWSCDKStack("Stack")
            .WithReference(awsSdkConfig)
            .Resource;

        Assert.Equal("Stack", resource.Name);
        Assert.NotNull(resource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest1, resource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", resource.AWSSDKConfig.Profile);
    }

    [Fact]
    [RequiresTools(["node"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public void AddAWSCDKResourceWithAdditionalStackTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var awsSdkConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest1)
            .WithProfile("test-profile");

        var cdk = builder
            .AddAWSCDKStack("Stack")
            .WithReference(awsSdkConfig);
        var resource = builder
            .AddAWSCDKStack("Other")
            .WithReference(awsSdkConfig).Resource;

        Assert.Equal("Other", resource.Name);
        Assert.NotNull(resource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest1, resource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", resource.AWSSDKConfig.Profile);
    }

    [Fact]
    [RequiresTools(["node"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public void AddAWSCDKResourceWithAdditionalStackAndConfigTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var awsSdkConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest1)
            .WithProfile("test-profile");
        var awsSdkStackConfig = builder.AddAWSSDKConfig()
            .WithRegion(RegionEndpoint.EUWest2)
            .WithProfile("other-test-profile");

        var cdk = builder.AddAWSCDKStack("Stack")
            .WithReference(awsSdkConfig);
        var cdkResource = cdk.Resource;
        var stackResource = builder.AddAWSCDKStack("Other").WithReference(awsSdkStackConfig).Resource;

        // Assert Stack resource
        Assert.Equal("Other", stackResource.Name);
        Assert.NotNull(stackResource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest2, stackResource.AWSSDKConfig.Region);
        Assert.Equal("other-test-profile", stackResource.AWSSDKConfig.Profile);

        // Assert CDK resource
        Assert.Equal("Stack", cdkResource.Name);
        Assert.NotNull(cdkResource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.EUWest1, cdkResource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", cdkResource.AWSSDKConfig.Profile);
    }

    [Fact]
    [RequiresTools(["node"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public void AddAWSCDKResourceWithConstructTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cdk = builder.AddAWSCDKStack("Stack");
        var resource = cdk.AddConstruct("Construct", scope => new Construct(scope, "Construct")).Resource;

        Assert.Equal("Construct", resource.Name);
        Assert.Equal(cdk.Resource, resource.Parent);
    }

    [Fact]
    [RequiresTools(["node"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task ManifestAWSCDKResourceTest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cdk = builder.AddAWSCDKStack("Stack");
        var resourceBuilder = cdk.AddConstruct("Construct", scope => new Bucket(scope, "Bucket"));

        builder.AddProject<Projects.ServiceA>("ServiceA")
            .WithReference(resourceBuilder, bucket => bucket.BucketName, "BucketName");

        var resource = cdk.Resource;
        Assert.NotNull(resource);

        const string expectedManifest = """
                                        {
                                          "type": "aws.cloudformation.template.v0",
                                          "stack-name": "Stack",
                                          "template-path": "cdk.out/Stack.template.json",
                                          "references": [
                                            {
                                              "target-resource": "ServiceA"
                                            }
                                          ]
                                        }
                                        """;

        var manifest = await ManifestUtils.GetManifest(resource);
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
