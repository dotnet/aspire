// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Aspire.Hosting.AWS.CloudFormation.Functions;

namespace Aspire.Hosting.AWS.CloudFormation.Constructs;

/// <summary>
/// Represents an Amazon SNS topic resource, including topic properties and associated subscriptions.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsSnsTopicConstruct(string name) : AwsConstruct(name)
{
    public override string Type => "AWS::SNS::Topic";
    public new TopicProperties Properties { get; init; } = new();

    public class TopicProperties : Properties
    {
        public string? TopicName { get; init; }
        public string? DisplayName { get; init; }

        [JsonPropertyName("Subscription")]
        public List<Subscription> Subscriptions { get; init; } = new();
    }

    public class Subscription(string protocol, object endpoint)
    {
        public string Protocol { get; init; } = protocol;
        public object Endpoint { get; init; } = endpoint;
    }

    /// <summary>
    /// Adds a subscriber to the SNS topic.
    /// </summary>
    /// <param name="awsConstruct">The AWS resource to subscribe to the topic.</param>
    /// <remarks>
    /// TODO: Extend this method to support other types of subscribers.
    /// </remarks>
    public void AddSubscriber(IAwsConstruct awsConstruct)
    {
        // TODO: Check if the resource is a valid type (SNS, SQS, HTTP, etc.)
        if (awsConstruct is AwsSqsQueueConstruct)
        {
            // TODO: Check if it's an existing resource. Maybe adding ARN to the resource class?

            var getAtt = new FnGetAtt(awsConstruct.Name, "Arn");

            var subscription = new Subscription("sqs", getAtt) { Protocol = "sqs", Endpoint = getAtt };
            Properties.Subscriptions.Add(subscription);
        }
        // TODO: Add checks for other resource types
    }
}
