// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an AWS S3 bucket resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AwsS3BucketResource(string name) : Resource(name), IAwsResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the name of the S3 bucket.
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    ///  Gets or sets the access control of the S3 bucket.
    /// </summary>
    public string? AccessControl { get; set; }

    /// <summary>
    ///  Gets or sets the Amazon Resource Name (ARN) of the S3 Bucket.
    /// </summary>
    public string? Arn { get; set; }

    /// <summary>
    ///  Gets the name of the S3 bucket resource.
    ///  </summary>
    ///  <returns>The name of the S3 bucket resource.</returns>
    public string? GetConnectionString() => BucketName;
}
