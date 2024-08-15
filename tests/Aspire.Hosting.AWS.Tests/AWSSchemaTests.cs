// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Aspire.Hosting.Tests.Schema;
using Xunit;

namespace Aspire.Hosting.AWS.Tests;

public class AWSSchemaTests
{
    [Fact]
    public void ValidateAddAWSCloudFormationStackManifest()
    {
        new SchemaTests().ValidateApplicationSamples("AwsStack", (IDistributedApplicationBuilder builder) =>
        {
            var awsSdkConfig = builder.AddAWSSDKConfig()
                                      .WithRegion(RegionEndpoint.USWest2)
                                      .WithProfile("test-profile");

            builder.AddAWSCloudFormationStack("ExistingStack")
                   .WithReference(awsSdkConfig);
        });
    }

    [Fact]
    public void ValidateAddAWSCloudFormationTemplateManifest()
    {
        new SchemaTests().ValidateApplicationSamples("AwsTemplate", (IDistributedApplicationBuilder builder) =>
        {
            var awsSdkConfig = builder.AddAWSSDKConfig()
                                      .WithRegion(RegionEndpoint.USWest2)
                                      .WithProfile("test-profile");

            builder.AddAWSCloudFormationTemplate("TemplateStack", "nonexistenttemplate")
                   .WithReference(awsSdkConfig);
        });
    }
}
