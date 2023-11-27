// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding AWS resources to the application model.
/// </summary>
public static class AwsResourceExtensions
{
    /// <summary>
    /// Adds an AWS S3 bucket resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A resource builder for the S3 bucket resource.</returns>
    public static IResourceBuilder<AwsS3BucketResource> AddAwsS3Bucket(this IDistributedApplicationBuilder builder, string name)
    {
        var s3Bucket = new AwsS3BucketResource(name);
        return builder.AddResource(s3Bucket)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteS3BucketToManifest));
    }

    /// <summary>
    /// Adds an AWS SQS queue resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A resource builder for the SQS queue resource.</returns>
    public static IResourceBuilder<AwsSqsQueueResource> AddAwsSqsQueue(this IDistributedApplicationBuilder builder, string name)
    {
        var sqsQueue = new AwsSqsQueueResource(name);
        return builder.AddResource(sqsQueue)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteSqsQueueToManifest));
    }

    /// <summary>
    /// Adds an AWS SNS topic resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A resource builder for the SNS topic resource.</returns>
    public static IResourceBuilder<AwsSnsTopicResource> AddAwsSnsTopic(this IDistributedApplicationBuilder builder, string name)
    {
        var snsTopic = new AwsSnsTopicResource(name);
        return builder.AddResource(snsTopic)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteSnsTopicToManifest));
    }

    private static void WriteS3BucketToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "aws.s3.bucket");
    }

    private static void WriteSqsQueueToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "aws.sqs.queue");
    }

    private static void WriteSnsTopicToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "aws.sns.topic");
    }
}
