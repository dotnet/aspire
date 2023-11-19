// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS SQS queue resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsSqsQueueResource(string name) : Resource(name), IAwsResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the URI of the SQS queue.
    /// </summary>
    public Uri? QueueUrl { get; set; }

    /// <summary>
    ///  Gets or sets the Amazon Resource Name (ARN) of the Sqs.
    /// </summary>
    public string? Arn { get; set; }

    /// <summary>
    ///  Gets the url of the SQS queue resource.
    /// </summary>
    ///  <returns>The url of the SQS queue resource.</returns>
    public string? GetConnectionString() => QueueUrl?.ToString();
}
