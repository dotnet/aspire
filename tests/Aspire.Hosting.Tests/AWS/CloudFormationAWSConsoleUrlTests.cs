// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CloudFormation;
using Xunit;

namespace Aspire.Hosting.Tests.AWS;

public class CloudFormationAWSConsoleUrlTests
{
    [Fact]
    public void Test2()
    {
        const string stackId = "arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example";

        var url = CloudFormationAWSConsoleUrlMapper.MapCloudFormationUrl(stackId);

        Assert.Equal("https://eu-west-1.console.aws.amazon.com/cloudformation/home?region=eu-west-1#/stacks/resources?stackId=arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example",url);
    }

    [Theory]
    [InlineData("AWS::S3::Bucket", "my-bucket","https://eu-west-1.console.aws.amazon.com/s3/buckets/my-bucket?region=eu-west-1")]
    [InlineData("AWS::SQS::Queue", "my-queue","https://eu-west-1.console.aws.amazon.com/sqs/v3/home?region=eu-west-1#/queues/my-queue")]
    [InlineData("AWS::SNS::Topic", "my-topic","https://eu-west-1.console.aws.amazon.com/sns/v3/home?region=eu-west-1#/topic/my-topic")]
    [InlineData("AWS::DynamoDB::Table", "my-table","https://eu-west-1.console.aws.amazon.com/dynamodbv2/home?region=eu-west-1#item-explorer?table=my-table")]
    [InlineData("AWS::DynamoDB::GlobalTable", "my-table","https://eu-west-1.console.aws.amazon.com/dynamodbv2/home?region=eu-west-1#item-explorer?table=my-table")]
    [InlineData("AWS::SQS::QueuePolicy", "policy")]
    public void Test(string resourceType, string physicalResourceId, string? resolvedUrl = default)
    {
        const string stackId = "arn:aws:cloudformation:eu-west-1:111111111111:stack/Stack1/abcdef-example";

        var url = CloudFormationAWSConsoleUrlMapper.MapResourceUrl(stackId, resourceType, physicalResourceId);

        Assert.Equal(resolvedUrl, url);
    }
}
