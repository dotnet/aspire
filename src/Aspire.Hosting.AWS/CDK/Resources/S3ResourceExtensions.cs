using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Notifications;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Amazon S3 resources to the application model.
/// </summary>
public static class S3ResourceExtensions
{

    private const string BucketNameOutputName = "BucketName";

    /// <summary>
    /// Adds an Amazon S3 bucket.
    /// </summary>
    /// <param name="builder">The builder for the AWS CDK stack.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="props">The properties of the bucket.</param>
    public static IResourceBuilder<IConstructResource<Bucket>> AddS3Bucket(this IResourceBuilder<IStackResource> builder, string name, IBucketProps? props = null)
    {
        return builder.AddConstruct(name, scope => new Bucket(scope, name, props));
    }

    /// <summary>Subscribes a destination to receive notifications when an object is created in the bucket.</summary>
    /// <param name="builder">The builder for the bucket resource.</param>
    /// <param name="destination">The notification destination queue.</param>
    /// <param name="eventType">The type of bucket event.</param>
    /// <param name="filters">Filters.</param>
    public static IResourceBuilder<IConstructResource<Bucket>> AddEventNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, EventType eventType, params INotificationKeyFilter[] filters)
    {
        builder.Resource.Construct.AddEventNotification(eventType, new SqsDestination(destination.Resource.Construct), filters);
        return builder;
    }

    /// <summary>Subscribes a destination to receive notifications when an object is created in the bucket.</summary>
    /// <param name="builder">The builder for the bucket resource.</param>
    /// <param name="destination">The notification destination queue.</param>
    /// <param name="filters">Filters.</param>
    public static IResourceBuilder<IConstructResource<Bucket>> AddObjectCreatedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, params INotificationKeyFilter[] filters)
    {
        builder.Resource.Construct.AddObjectCreatedNotification(new SqsDestination(destination.Resource.Construct), filters);
        return builder;
    }

    /// <summary>Subscribes a destination to receive notifications when an object is created in the bucket.</summary>
    /// <param name="builder">The builder for the bucket resource.</param>
    /// <param name="destination">The notification destination topic.</param>
    /// <param name="filters">Filters.</param>
    public static IResourceBuilder<IConstructResource<Bucket>> AddObjectCreatedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<ITopic>> destination, params INotificationKeyFilter[] filters)
    {
        builder.Resource.Construct.AddObjectCreatedNotification(new SnsDestination(destination.Resource.Construct), filters);
        return builder;
    }

    /// <summary>Subscribes a destination to receive notifications when an object is removed from the bucket.</summary>
    /// <param name="builder">The builder for the bucket resource.</param>
    /// <param name="destination">The notification destination queue.</param>
    /// <param name="filters">Filters.</param>
    public static IResourceBuilder<IConstructResource<Bucket>> AddObjectRemovedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<IQueue>> destination, params INotificationKeyFilter[] filters)
    {
        builder.Resource.Construct.AddObjectCreatedNotification(new SqsDestination(destination.Resource.Construct), filters);
        return builder;
    }

    /// <summary>Subscribes a destination to receive notifications when an object is removed from the bucket.</summary>
    /// <param name="builder">The builder for the bucket resource.</param>
    /// <param name="destination">The notification destination topic.</param>
    /// <param name="filters">Filters.</param>
    public static IResourceBuilder<IConstructResource<Bucket>> AddObjectRemovedNotification(this IResourceBuilder<IConstructResource<Bucket>> builder, IResourceBuilder<IConstructResource<ITopic>> destination, params INotificationKeyFilter[] filters)
    {
        builder.Resource.Construct.AddObjectRemovedNotification(new SnsDestination(destination.Resource.Construct), filters);
        return builder;
    }

    /// <summary>
    /// Adds a reference of an Amazon S3 bucket to a project. The output parameters of the bucket are added to the project IConfiguration.
    /// </summary>
    /// <param name="builder">The builder for the resource.</param>
    /// <param name="bucket">The Amazon S3 bucket resource.</param>
    /// <param name="configSection">The optional config section in IConfiguration to add the output parameters.</param>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Bucket>> bucket, string? configSection = null)
        where TDestination : IResourceWithEnvironment
    {
        configSection ??= $"{Constants.DefaultConfigSection}:{bucket.Resource.Name}";
        var prefix = configSection.ToEnvironmentVariables();
        return builder.WithEnvironment($"{prefix}__{BucketNameOutputName}", bucket, b => b.BucketName, BucketNameOutputName);
    }
}
