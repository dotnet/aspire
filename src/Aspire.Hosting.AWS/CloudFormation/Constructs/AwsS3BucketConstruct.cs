// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CloudFormation.Constructs;

/// <summary>
/// Represents an Amazon S3 bucket resource, providing properties and configurations specific to S3.
/// </summary>
/// <param name="name">The name of the resource.</param>
internal class AwsS3BucketConstruct(string name) : AwsConstruct(name)
{
    public override string Type => "AWS::S3::Bucket";

    public new BucketProperties Properties { get; init; } = new();

    public class BucketProperties : Properties
    {
        public string? BucketName { get; set; }
        public string? AccessControl { get; set; }
        public VersioningConfiguration? VersioningConfiguration { get; set; }
    }

    public class VersioningConfiguration
    {
        public string? Status { get; init; }
    }
}
