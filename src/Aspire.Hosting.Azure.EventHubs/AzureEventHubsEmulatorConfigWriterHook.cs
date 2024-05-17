// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using System.Text.Json;

namespace Aspire.Hosting.Azure;

internal sealed class AzureEventHubsEmulatorConfigWriterHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var eventHubsEmulatorResources = appModel.Resources.OfType<AzureEventHubsResource>().Where(x => x is { } eventHubsResource && eventHubsResource.IsEmulator);

        if (!eventHubsEmulatorResources.Any())
        {
            // No-op if there is no Azure Event Hubs emulator resource.
            return Task.CompletedTask;
        }

        foreach (var emulatorResource in eventHubsEmulatorResources)
        {
            var configFileMount = emulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single(v => v.Target == AzureEventHubsEmulatorResource.EmulatorConfigJsonPath);

            using var stream = new FileStream(configFileMount.Source!, FileMode.Create);
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();                      // {
            writer.WriteStartObject("UserConfig");          //   "UserConfig": {
            writer.WriteStartArray("NamespaceConfig");      //     "NamespaceConfig": [
            writer.WriteStartObject();                      //       {
            writer.WriteString("Type", "EventHub");         //         "Type": "EventHub",
            writer.WriteString("Name", "emulatorNs1");      //         "Name": "emulatorNs1"
            writer.WriteStartArray("Entities");             //         "Entities": [

            foreach (var hub in emulatorResource.Hubs)
            {
                // The default consumer group ('$default') is automatically created

                writer.WriteStartObject();                  //           {
                writer.WriteString("Name", hub.Name);       //             "Name": "hub",
                writer.WriteString("PartitionCount", "2");  //             "PartitionCount": "2",
                writer.WriteStartArray("ConsumerGroups");   //             "ConsumerGroups": [
                writer.WriteEndArray();                     //             ]
                writer.WriteEndObject();                    //           }
            }

            writer.WriteEndArray();                         //         ] (/Entities)
            writer.WriteEndObject();                        //       } 
            writer.WriteEndArray();                         //     ], (/NamespaceConfig)
            writer.WriteStartObject("LoggingConfig");       //     "LoggingConfig": {
            writer.WriteString("Type", "File");             //       "Type": "File"
            writer.WriteEndObject();                        //     } (/LoggingConfig)

            writer.WriteEndObject();                        //   } (/UserConfig)
            writer.WriteEndObject();                        // } (/Root)

        }

        return Task.CompletedTask;
    }
}
