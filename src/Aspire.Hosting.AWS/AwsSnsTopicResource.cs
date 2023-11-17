// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS SNS topic resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsSnsTopicResource(string name) : Resource(name), IAwsResource
{
    /// <summary>
    /// Gets or sets the ARN of the SNS topic.
    /// </summary>
    public string? TopicArn { get; set; }
}
