// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS S3 bucket resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsS3BucketResource(string name) : Resource(name), IAwsResource
{
    /// <summary>
    /// Gets or sets the name of the S3 bucket.
    /// </summary>
    public string? BucketName { get; set; }
}
