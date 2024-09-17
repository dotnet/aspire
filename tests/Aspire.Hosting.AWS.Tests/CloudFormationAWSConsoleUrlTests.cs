// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.CloudFormation;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.AWS.Provisioning;
using Xunit;

namespace Aspire.Hosting.AWS.Tests;

public class CloudFormationAWSConsoleUrlTests
{
    [Fact]
    public void ConsoleUrlCreated_RegionEndpoint()
    {
        const string stackId = "arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example";
        using var client = new AmazonCloudFormationClient(RegionEndpoint.EUWest1);

        var urls = CloudFormationResourceProvisioner<CloudFormationTemplateResource>.MapCloudFormationStackUrl(client, stackId);

        Assert.Equal(
            "https://console.aws.amazon.com/cloudformation/home?region=eu-west-1#/stacks/resources?stackId=arn%3Aaws%3Acloudformation%3Aeu-west-1%3A111111111111%3Astack%2FStack1%2Fabcdef-example",
            urls!.Value.Single().Url);
    }

    [Fact]
    public void ConsoleUrlCreated_ServiceUrl()
    {
        const string stackId = "arn:aws:cloudformation:ap-southeast-1:111111111111:stack/Stack1/abcdef-example";
        using var client = new AmazonCloudFormationClient(new AmazonCloudFormationConfig
        {
            ServiceURL = "https://cloudformation.ap-southeast-1.amazonaws.com/"
        });

        var urls = CloudFormationResourceProvisioner<CloudFormationTemplateResource>.MapCloudFormationStackUrl(client, stackId);

        Assert.Equal(
            "https://console.aws.amazon.com/cloudformation/home?region=ap-southeast-1#/stacks/resources?stackId=arn%3Aaws%3Acloudformation%3Aap-southeast-1%3A111111111111%3Astack%2FStack1%2Fabcdef-example",
            urls!.Value.Single().Url);
    }

    [Fact]
    public void ConsoleUrlNotCreated_LocalStackServiceUrl()
    {
        using var client = new AmazonCloudFormationClient(new AmazonCloudFormationConfig
        {
            ServiceURL = "http://localhost:4566",
        });

        const string stackId = "arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example";

        var urls = CloudFormationResourceProvisioner<CloudFormationTemplateResource>.MapCloudFormationStackUrl(client, stackId);
        Assert.Null(urls);
    }

    [Fact]
    public void ConsoleUrlNotCreated_UnknownRegion()
    {
        using var client = new AmazonCloudFormationClient(new AmazonCloudFormationConfig
        {
            ServiceURL = "https://cloudformation.example-north-1.amazonaws.example.com/",
        });

        const string stackId = "arn:aws:cloudformation:example-north-1:111111111111:stack/Stack1/abcdef-example";

        var urls = CloudFormationResourceProvisioner<CloudFormationTemplateResource>.MapCloudFormationStackUrl(client, stackId);
        Assert.Null(urls);
    }
}
