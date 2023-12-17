// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Aspire.Hosting.Dashboard;

internal class DashboardService : V1.DashboardService.DashboardServiceBase
{
    private readonly IResourceService _resourceService;

    public DashboardService(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public override Task<ResourceCommandResponse> ExecuteResourceCommand(
        ResourceCommandRequest request,
        ServerCallContext context)
    {
        // TODO implement command handling
        Console.WriteLine($"Command \"{request.CommandType}\" requested for resource \"{request.ResourceId.Uid}\" ({request.ResourceId.ResourceType})");

        return Task.FromResult(new ResourceCommandResponse { Kind = ResourceCommandResponseKind.Succeeded });
    }

    public override Task<ApplicationInformationResponse> GetApplicationInformation(
        ApplicationInformationRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new ApplicationInformationResponse
        {
            ApplicationName = _resourceService.ApplicationName,
            ApplicationVersion = "0.0.0" // TODO obtain correct version
        });
    }

    public override async Task WatchResources(
        WatchResourcesRequest request,
        IServerStreamWriter<WatchResourcesUpdate> responseStream,
        ServerCallContext context)
    {
        var channel = Channel.CreateUnbounded<WatchResourcesUpdate>();

        // Send data
        _ = Task.Run(async () =>
        {
            // Initial snapshot
            var initialSnapshot = new WatchResourcesUpdate
            {
                InitialData = new InitialResourceData
                {
                    Resources =
                    {
                        CreateRandomResourceSnapshot("One"),
                        CreateRandomResourceSnapshot("Two")
                    },
                    ResourceTypes =
                    {
                        new ResourceType { UniqueName = "test", DisplayName = "Test", Commands = { } }
                    }
                }
            };

            await channel.Writer.WriteAsync(initialSnapshot).ConfigureAwait(false);

            // Send random updates
            while (true)
            {
                await Task.Delay(3000).ConfigureAwait(false);

                var update = new WatchResourcesUpdate
                {
                    Changes = new WatchResourcesChanges
                    {
                        Value =
                        {
                            new WatchResourcesChange { Upsert = CreateRandomResourceSnapshot("One") }
                        }
                    }
                };

                await channel.Writer.WriteAsync(update).ConfigureAwait(false);
            }
        });

        await foreach (var update in channel.Reader.ReadAllAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(update, context.CancellationToken).ConfigureAwait(false);
        }
    }

    private static V1.Resource CreateRandomResourceSnapshot(string id)
    {
        // Construct dummy data
        return new()
        {
            ResourceId = new()
            {
                Uid = id,
                ResourceType = "test"
            },
            DisplayName = id,
            State = "Running",
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.Date),
            ExpectedEndpointsCount = 2,
            Endpoints =
            {
                new Endpoint { EndpointUrl = "endpoint", ProxyUrl = "http://proxy" },
            },
            Services =
            {
                new Service { Name = "service1", HttpAddress = "http://service1" },
                new Service { Name = "service2", AllocatedAddress = "service2", AllocatedPort = 1234 }
            },
            Environment =
            {
                new EnvironmentVariable { Name = "key", Value = "value" }
            },
            AdditionalData =
            {
                new AdditionalData { Namespace = "test", Name = "dummy1", Value = Value.ForString("foo") },
                new AdditionalData { Namespace = "test", Name = "dummy2", Value = Value.ForList(Value.ForString("foo"), Value.ForString("bar")) },
            }
        };
    }
}
