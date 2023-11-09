// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class MongoDBExtensions
{
    public static IResourceBuilder<MongoDBContainerResource> AddMongoDBContainer(this IDistributedApplicationBuilder builder, string name, string? password = null)
    {
        password = password ?? Guid.NewGuid().ToString("N");
        var mongo = new MongoDBContainerResource(name, password);
        return builder.AddResource(mongo)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteMongoDBContainerToManifest))
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, containerPort: 27017))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mongo", Tag = "latest" });
    }

    public static IResourceBuilder<MongoDBContainerResource> WithMongoExpress(this IResourceBuilder<MongoDBContainerResource> builder)
    {
        builder.ApplicationBuilder.AddContainer("mongo-express", "mongo-express", "latest")
                                  .WithServiceBinding(8081, 8081, scheme: "http")
                                  .WithEnvironment((context) =>
                                  {
                                      if (builder.Resource.GetConnectionString() is not { } connectionString)
                                      {
                                          throw new DistributedApplicationException($"MongoDBContainer resource '{builder.Resource.Name}' did not return a connection string.");
                                      }

                                      var connectionStringUri = new Uri(connectionString);
                                      context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_SERVER", connectionStringUri.Host);
                                      context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_PORT", connectionStringUri.Port.ToString(CultureInfo.InvariantCulture));
                                  });

        return builder;
    }

    private static void WriteMongoDBContainerToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "mongodb.server.v0");
    }
}
