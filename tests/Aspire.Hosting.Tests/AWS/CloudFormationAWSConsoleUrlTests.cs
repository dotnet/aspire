// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.CloudFormation;
using Aspire.Hosting.AWS.CloudFormation;
using Xunit;

namespace Aspire.Hosting.Tests.AWS;

public class CloudFormationAWSConsoleUrlTests
{
    [Fact]
    public void ConsoleUrlCreated()
    {
        const string stackId = "arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example";
        using var client = new AmazonCloudFormationClient(RegionEndpoint.EUWest1);

        var urls = CloudFormationProvisioner.MapCloudFormationStackUrl(client, stackId);

        Assert.Equal(
            "https://console.aws.amazon.com/cloudformation/home?region=eu-west-1#/stacks/resources?stackId=arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example",
            urls!.Value.Single().Url);
    }

    [Fact]
    public void ConsoleUrlNotCreated()
    {
        using var client = new AmazonCloudFormationClient(new AmazonCloudFormationConfig
        {
            ServiceURL = "http://localhost:4566",
        });

        const string stackId = "arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example";

        var urls = CloudFormationProvisioner.MapCloudFormationStackUrl(client, stackId);
        Assert.Null(urls);
    }
}
