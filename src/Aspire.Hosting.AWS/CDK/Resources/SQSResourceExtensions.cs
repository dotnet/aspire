using Amazon.CDK.AWS.SQS;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Amazon SQS resources to the application model.
/// </summary>
public static class SQSResourceExtensions
{

    private const string QueueUrlOutputName = "QueueUrl";

    /// <summary>
    /// Adds an Amazon SQS queue.
    /// </summary>
    /// <param name="builder">The builder for the AWS CDK stack.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="props">The properties of the queue.</param>
    public static IResourceBuilder<IConstructResource<Queue>> AddSQSQueue(this IResourceBuilder<IStackResource> builder, string name, IQueueProps? props = null)
    {
        return builder.AddConstruct(name, scope => new Queue(scope, name, props));
    }

    /// <summary>
    /// Adds a reference of an Amazon SQS queue to a project. The output parameters of the queue are added to the project IConfiguration.
    /// </summary>
    /// <param name="builder">The builder for the resource.</param>
    /// <param name="queue">The Amazon SQS queue resource.</param>
    /// <param name="configSection">The optional config section in IConfiguration to add the output parameters.</param>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Queue>> queue, string? configSection = null)
        where TDestination : IResourceWithEnvironment
    {
        configSection ??= $"{Constants.DefaultConfigSection}:{queue.Resource.Name}";
        var prefix = configSection.ToEnvironmentVariables();
        return builder.WithEnvironment($"{prefix}__{QueueUrlOutputName}", queue, q => q.QueueUrl, QueueUrlOutputName);
    }
}
