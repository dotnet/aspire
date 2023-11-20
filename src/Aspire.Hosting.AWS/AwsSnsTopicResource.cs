// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS SNS topic resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsSnsTopicResource(string name) : Resource(name), IAwsResource, IResourceWithConnectionString
{
    /// <summary>
    ///  Gets or sets the Amazon Resource Name (ARN) of the SNS.
    /// </summary>
    public string? Arn { get; set; }

    /// <summary>
    ///  Gets or sets the name of the SNS topic.
    /// </summary>
    public string? TopicName { get; set; }

    /// <summary>
    ///  Gets or sets the subscriptions of the SNS.
    /// </summary>
    public IList<string> Subscriptions { get; set; } = new List<string>();

    /// <summary>
    ///  Gets the name of the SNS topic resource.
    ///  </summary>
    ///  <returns>The name of the SNS topic resource.</returns>
    public string? GetConnectionString() => Arn;
}
