// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.Provisioning;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.AWS.Tests;

#pragma warning disable IDE0200 //Remove unnecessary lambda expression (IDE0200)
public class AWSPublicApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorAWSProvisioningExceptionShouldThrowWhenMessageIsNullOrEmpty(bool isNull)
    {
        var message = isNull ? null! : string.Empty;

        var action = () => new AWSProvisioningException(message, default(Exception?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(message), exception.ParamName);
    }

    [Fact]
    public void AddAWSSDKConfigShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddAWSSDKConfig();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithProfileShouldThrowWhenConfigIsNull()
    {
        IAWSSDKConfig config = null!;
        var profile = "default";

        var action = () => config.WithProfile(profile);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(config), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithProfileShouldThrowWhenProfileIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var config = builder.AddAWSSDKConfig();
        var profile = isNull ? null! : string.Empty;

        var action = () => config.WithProfile(profile);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(profile), exception.ParamName);
    }

    [Fact]
    public void WithRegionShouldThrowWhenConfigIsNull()
    {
        IAWSSDKConfig config = null!;
        var region = RegionEndpoint.EUCentral1;

        var action = () => config.WithRegion(region);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(config), exception.ParamName);
    }

    [Fact]
    public void WithRegionShouldThrowWhenRegionIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var config = builder.AddAWSSDKConfig();
        RegionEndpoint region = null!;

        var action = () => config.WithRegion(region);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(region), exception.ParamName);
    }

    [Fact]
    public void WithReferenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<ProjectResource> builder = null!;
        var testBuilder = TestDistributedApplicationBuilder.Create();
        var awsSdkConfig = testBuilder.AddAWSSDKConfig();

        var action = () => builder.WithReference(awsSdkConfig);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithReferenceShouldThrowWhenAwsSdkConfigIsNull()
    {
        var testBuilder = TestDistributedApplicationBuilder.Create();
        var resource = new KafkaServerResource("kafka");
        var builder = testBuilder.AddResource(resource);

        IAWSSDKConfig awsSdkConfig = null!;

        var action = () => builder.WithReference(awsSdkConfig);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(awsSdkConfig), exception.ParamName);
    }
}
