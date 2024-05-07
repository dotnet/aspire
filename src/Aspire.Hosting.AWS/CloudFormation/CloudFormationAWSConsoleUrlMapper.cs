// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation;

internal static class CloudFormationAWSConsoleUrlMapper
{
    public static string MapCloudFormationUrl(string stackId)
    {
        var region = stackId.Split(":")[3];

        return
            $"https://{region}.console.aws.amazon.com/cloudformation/home?region={region}#/stacks/resources?stackId={stackId}";
    }

    public static string? MapResourceUrl(string stackId, string resourceType, string physicalResourceId)
    {
        var region = stackId.Split(":")[3];

        return resourceType switch
        {
            "AWS::SNS::Topic" =>
                $"https://{region}.console.aws.amazon.com/sns/v3/home?region={region}#/topic/{physicalResourceId}",
            "AWS::SQS::Queue" =>
                $"https://{region}.console.aws.amazon.com/sqs/v3/home?region={region}#/queues/{physicalResourceId}",
            "AWS::S3::Bucket" =>
                $"https://{region}.console.aws.amazon.com/s3/buckets/{physicalResourceId}?region={region}",
            "AWS::DynamoDB::Table" or "AWS::DynamoDB::GlobalTable" =>
                $"https://{region}.console.aws.amazon.com/dynamodbv2/home?region={region}#item-explorer?table={physicalResourceId}",
            "AWS::Cognito::UserPool" =>
                $"https://{region}.console.aws.amazon.com/cognito/v2/idp/user-pools/{physicalResourceId}/users?region={region}",
            _ => null
        };
    }
}
