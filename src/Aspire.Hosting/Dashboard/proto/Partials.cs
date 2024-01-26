// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.V1;

partial class Resource
{
    public static Resource FromSnapshot(ResourceSnapshot snapshot)
    {
        Resource resource = new()
        {
            Name = snapshot.Name,
            ResourceType = snapshot.ResourceType,
            DisplayName = snapshot.DisplayName,
            Uid = snapshot.Uid,
            State = snapshot.State,
        };

        if (snapshot.CreationTimeStamp.HasValue)
        {
            resource.CreatedAt = Timestamp.FromDateTime(snapshot.CreationTimeStamp.Value.ToUniversalTime());
        }

        if (snapshot.ExpectedEndpointsCount.HasValue)
        {
            resource.ExpectedEndpointsCount = snapshot.ExpectedEndpointsCount.Value;
        }

        foreach (var env in snapshot.Environment)
        {
            resource.Environment.Add(new EnvironmentVariable { Name = env.Name, Value = env.Value ?? "", IsFromSpec = env.IsFromSpec });
        }

        foreach (var endpoint in snapshot.Endpoints)
        {
            resource.Endpoints.Add(new Endpoint { EndpointUrl = endpoint.EndpointUrl, ProxyUrl = endpoint.ProxyUrl });
        }

        foreach (var service in snapshot.Services)
        {
            var serviceMessage = new Service { Name = service.Name };

            if (service.AllocatedAddress is not null)
            {
                serviceMessage.AllocatedAddress = service.AllocatedAddress;
            }

            if (service.AllocatedPort.HasValue)
            {
                serviceMessage.AllocatedPort = service.AllocatedPort.Value;
            }

            resource.Services.Add(serviceMessage);
        }

        foreach (var property in snapshot.Properties)
        {
            resource.Properties.Add(new ResourceProperty { Name = property.Name, Value = property.Value });
        }

        return resource;
    }
}
