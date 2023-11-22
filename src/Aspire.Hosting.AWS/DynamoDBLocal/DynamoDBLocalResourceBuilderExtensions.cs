// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.DynamoDBLocal;

namespace Aspire.Hosting;

public static class DynamoDBLocalResourceBuilderExtensions
{
    /// <summary>
    /// Add an instance of DynamoDB local. This is a container pulled from Amazon ECR public gallery.
    /// Projects that use DynamoDB local can get a reference to the instance using the WithAWSDynamoDBLocalReference method.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="image">Optional: default value is "public.ecr.aws/aws-dynamodb-local/aws-dynamodb-local"</param>
    /// <param name="tag">Optional: default value is "latest"</param>
    /// <param name="disableDynamoDBLocalTelemetry">Optional: default to false. It controls setting the DynamoDB local DDB_LOCAL_TELEMETRY environment variable.</param>
    /// <returns></returns>
    /// <exception cref="DistributedApplicationException"></exception>
    public static IResourceBuilder<IDynamoDBLocalResource> AddAWSDynamoDBLocal(this IDistributedApplicationBuilder builder,
        string name, string image = "public.ecr.aws/aws-dynamodb-local/aws-dynamodb-local", string tag = "latest", bool disableDynamoDBLocalTelemetry = false)
    {
        var container = new DynamoDBLocalResource(name, disableDynamoDBLocalTelemetry);
        var containerBuilder = builder.AddResource(container)
                  .ExcludeFromManifest()
                  .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, uriScheme: "http", port: null, containerPort: 8000))
                  .WithAnnotation(new ContainerImageAnnotation { Image = image, Tag = tag })
                  // TODO: Where DynamoDB local will persist its data. This won't work till we figure in Aspire how to set the COMMAND of a container to direct
                  // DynamoDB local to use shared db instead of in memory.
                  .WithVolumeMount($"./dynamodb/{container.Name}", "/home/dynamodblocal/data");

        // Repurpose the WithEnvironment to invoke the users callback to seed DynamoDB local.
        // This needs a better mechanism to have a callback once a container has been started
        // so a hosting component can do initial configuration or seeding in the container.
        containerBuilder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                return;
            }

            if (container.DisableDynamoDBLocalTelemetry)
            {
                // Info on DynamoDB Local telemetry
                // https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DynamoDBLocalTelemetry.html
                context.EnvironmentVariables.Add("DDB_LOCAL_TELEMETRY", "0");
            }
        });

        return containerBuilder;
    }

    /// <summary>
    /// Add a callback to seed DynamoDB local with tables and data once the container is started.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="seedDynamoDBCallback"></param>
    /// <returns></returns>
    public static IResourceBuilder<IDynamoDBLocalResource> WithSeedDynamoDBCallback(this IResourceBuilder<IDynamoDBLocalResource> builder, Func<SeedDynamoDBUtilities, CancellationToken, Task> seedDynamoDBCallback)
    {
        builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                return;
            }

            if (!builder.Resource.TryGetAllocatedEndPoints(out var allocatedEndPoints))
            {
                throw new DistributedApplicationException("Expected allocated endpoints!");
            }

            var endpoint = allocatedEndPoints.Single();
            var serviceUrl = $"http://{endpoint.Address}:{endpoint.Port}/";

            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = serviceUrl
            };

            // TODO: This logic can be simplified to always use the example credentials once I'm able to adjust the
            // COMMAND used to startup the container. Then I can set the DynamoDB local database to not include the
            // Access Key ID as part of the file name.
            AWSCredentials credentials;
            try
            {
                credentials = FallbackCredentialsFactory.GetCredentials();
            }
            catch
            {
                // DynamoDB local just needs something that looks like credentials. These are not real credentials as you call with the "EXAMPLE" suffix.
                credentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
            }

            var ddbClient = new AmazonDynamoDBClient(config);

            // A problem with doing it at this point is there isn't access to get an 
            // ILogger from this point to allow logging in my seed utilities.
            var utilities = new SeedDynamoDBUtilities(ddbClient);

            // TODO: This should eventually be added to a real lifecycle hookup that would have its own CancellationTokenSource
            var cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                utilities.WaitTillContainerAvailable(cancellationTokenSource.Token);

                // TODO: Figure out to get an ILogger at this point so I can get logging without using Console.WriteLine
                await seedDynamoDBCallback(utilities, cancellationTokenSource.Token).ConfigureAwait(false);
            });
        });

        return builder;
    }

    /// <summary>
    /// Add a reference to the DynamoDB local to the project. This is done by setting the AWS_ENDPOINT_URL_DYNAMODB environment
    /// variable for the project to the http endpoint of the DynamoDB local container. Any DynamoDB service clients
    /// created in the project relying on endpoint resolution will pick up this environment variable and use it.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="builder"></param>
    /// <param name="dynamoDBLocalresourceBuilder"></param>
    /// <returns></returns>
    /// <exception cref="DistributedApplicationException"></exception>
    public static IResourceBuilder<TDestination> WithAWSDynamoDBLocalReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IDynamoDBLocalResource> dynamoDBLocalresourceBuilder)
        where TDestination : IResourceWithEnvironment
    {
        builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                return;
            }

            if (!dynamoDBLocalresourceBuilder.Resource.TryGetAllocatedEndPoints(out var allocatedEndPoints))
            {
                throw new DistributedApplicationException("Expected allocated endpoints!");
            }

            var endpoint = allocatedEndPoints.Single();
            var serviceUrl = $"http://{endpoint.Address}:{endpoint.Port}/";
            context.EnvironmentVariables.Add("AWS_ENDPOINT_URL_DYNAMODB", serviceUrl);
        });
        return builder;
    }
}
