// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests.AWS;
public class AWSCloudFormationResourceTests
{
    [Fact]
    public void AddAWSCloudFormationStackResourceTest()
    {
        var builder = DistributedApplication.CreateBuilder();

        var awsSdkConfig = builder.AddAWSSDKConfig()
                                .WithRegion(RegionEndpoint.USWest2)
                                .WithProfile("test-profile");

        var resource = builder.AddAWSCloudFormationStack("ExistingStack")
                                                    .WithReference(awsSdkConfig)
                                                    .Resource;

        Assert.Equal("ExistingStack", resource.Name);
        Assert.NotNull(resource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.USWest2, resource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", resource.AWSSDKConfig.Profile);
    }

    [Fact]
    public void AddAWSCloudFormationTemplateResourceTest()
    {
        var builder = DistributedApplication.CreateBuilder();

        var awsSdkConfig = builder.AddAWSSDKConfig()
                                .WithRegion(RegionEndpoint.USWest2)
                                .WithProfile("test-profile");

        var resource = builder.AddAWSCloudFormationTemplate("NewStack", "cf.template")
                                                    .WithParameter("key1", "value1")
                                                    .WithParameter("key2", "value2")
                                                    .WithReference(awsSdkConfig)
                                                    .Resource as CloudFormationTemplateResource;

        Assert.NotNull(resource);
        Assert.Equal("NewStack", resource.Name);
        Assert.Equal("cf.template", resource.TemplatePath);
        Assert.NotNull(resource.AWSSDKConfig);
        Assert.Equal(RegionEndpoint.USWest2, resource.AWSSDKConfig.Region);
        Assert.Equal("test-profile", resource.AWSSDKConfig.Profile);

        Assert.Equal(2, resource.CloudFormationParameters.Count);
        Assert.Equal("value1", resource.CloudFormationParameters["key1"]);
        Assert.Equal("value2", resource.CloudFormationParameters["key2"]);
    }

    [Fact]
    public async Task ManifestAWSCloudFormationStackResourceTest()
    {
        var builder = DistributedApplication.CreateBuilder();

        var resourceBuilder = builder.AddAWSCloudFormationStack("ExistingStack");

        builder.AddProject<Projects.ServiceA>("serviceA")
               .WithReference(resourceBuilder);

        var resource = resourceBuilder.Resource as CloudFormationStackResource;
        Assert.NotNull(resource);

        var expectedManifest = """
        {
          "type": "aws.cloudformation.stack.v0",
          "stack-name": "ExistingStack",
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
    public async Task ManifestAWSCloudFormationTemplateResourceTest()
    {
        var builder = DistributedApplication.CreateBuilder();

        var resourceBuilder = builder.AddAWSCloudFormationTemplate("NewStack", "cf.template");

        builder.AddProject<Projects.ServiceA>("serviceA")
               .WithReference(resourceBuilder);

        var resource = resourceBuilder.Resource as CloudFormationTemplateResource;
        Assert.NotNull(resource);

        var expectedManifest = """
        {
          "type": "aws.cloudformation.template.v0",
          "stack-name": "NewStack",
          "template-path": "cf.template",
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
}
