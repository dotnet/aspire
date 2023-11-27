// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CloudFormation.Functions;

namespace Aspire.Hosting.AWS.CloudFormation.Constructs;

/// <summary>
/// Represents an Amazon SQS queue resource, allowing specification of queue properties and settings.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsSqsQueueConstruct(string name) : AwsConstruct(name)
{
    public override string Type => "AWS::SQS::Queue";

    public new QueueProperties Properties { get; init; } = new();

    public class QueueProperties : Properties
    {
        public string? QueueName { get; init; }
        public int? VisibilityTimeout { get; init; }
        public int? MessageRetentionPeriod { get; init; }
    }

    public override IReadOnlyDictionary<string, CloudFormationTemplate.Output> GetOutputs()
    {
        return new Dictionary<string, CloudFormationTemplate.Output>()
        {
            { $"{Name}-QueueName", new CloudFormationTemplate.Output(new FnGetAtt(Name, "QueueName"), "SQS Name") },
            { $"{Name}-QueueURL", new CloudFormationTemplate.Output(new { Ref = Name }, "SQS Url") },
            { $"{Name}-QueueARN", new CloudFormationTemplate.Output(new FnGetAtt(Name, "Arn"), "SQS Arn") }
        };
    }
}
