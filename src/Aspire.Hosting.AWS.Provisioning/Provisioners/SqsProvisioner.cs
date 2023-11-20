// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation.Constructs;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.AWS.Provisioning.Provisioners;

internal sealed class SqsProvisioner : AwsResourceProvisioner<AwsSqsQueueResource, AwsSqsQueueConstruct>
{
    public override void ConfigureResource(IConfiguration configuration, AwsSqsQueueResource resource)
    {
        var sqsSection = configuration.GetSection($"AWS:SQS:{resource.Name}");

        var messageRetentionPeriod = sqsSection["MessageRetentionPeriod"];
        var visibilityTimeout = sqsSection["VisibilityTimeout"];

        // Should we throw if the value is not an integer?
        if (messageRetentionPeriod is not null && int.TryParse(messageRetentionPeriod, out var value))
        {
            resource.MessageRetentionPeriod = value;
        }

        if (visibilityTimeout is not null && int.TryParse(visibilityTimeout, out value))
        {
            resource.VisibilityTimeout = value;
        }
    }

    public override AwsSqsQueueConstruct CreateConstruct(AwsSqsQueueResource resource, ProvisioningContext context)
    {
        var awsSqsQueueConstruct = new AwsSqsQueueConstruct(resource.Name)
        {
            Properties = new AwsSqsQueueConstruct.QueueProperties()
            {
                MessageRetentionPeriod = resource.MessageRetentionPeriod, VisibilityTimeout = resource.VisibilityTimeout
            }
        };

        return awsSqsQueueConstruct;
    }

    public override void SetResourceOutputs(AwsSqsQueueResource resource, IImmutableDictionary<string, string> resourceOutputs)
    {
        resource.QueueName = resourceOutputs[$"{resource.Name}-QueueName"];
        resource.Arn = resourceOutputs[$"{resource.Name}-QueueARN"];
        resource.QueueUrl = new Uri(resourceOutputs[$"{resource.Name}-QueueURL"]);
    }
}
