// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS SQS queue resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsSqsQueueResource(string name) : Resource(name), IAwsResource
{
    /// <summary>
    /// Gets or sets the URI of the SQS queue.
    /// </summary>
    public Uri? QueueUrl { get; set; }
}
