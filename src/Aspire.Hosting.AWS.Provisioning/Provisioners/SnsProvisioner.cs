// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation.Constructs;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.AWS.Provisioning.Provisioners;

internal sealed class SnsProvisioner : AwsResourceProvisioner<AwsSnsTopicResource, AwsSnsTopicConstruct>
{
    private readonly IList<IAwsConstruct> _subscribedResources = new List<IAwsConstruct>();

    public override void ConfigureResource(IConfiguration configuration, AwsSnsTopicResource resource)
    {
        var snsSection = configuration.GetSection($"AWS:SNS:{resource.Name}");
        var subscriptions = new List<string>();
        snsSection.GetSection("Subscriptions").Bind(subscriptions);

        resource.Subscriptions = subscriptions;

        // TODO: add tags, etc.
    }

    public override AwsSnsTopicConstruct CreateConstruct(AwsSnsTopicResource resource, ProvisioningContext context)
    {
        var awsSnsTopicConstruct = new AwsSnsTopicConstruct(resource.Name);
        // Additional properties can be set here based on the resource definition

        foreach (var subscription in resource.Subscriptions)
        {
            var subscribedResource = _subscribedResources.FirstOrDefault(r => r.Name == subscription);
            if (subscribedResource is not null)
            {
                awsSnsTopicConstruct.AddSubscriber(subscribedResource);
            }
        }

        return awsSnsTopicConstruct;
    }

    public void AddSubscriptions(IAwsConstruct construct)
    {
        _subscribedResources.Add(construct);
    }

    public override void SetResourceOutputs(AwsSnsTopicResource resource, IImmutableDictionary<string, string> resourceOutputs)
    {
        resource.Arn = resourceOutputs[$"{resource.Name}-TopicARN"];
        resource.TopicName = resourceOutputs[$"{resource.Name}-TopicName"];
    }
}
