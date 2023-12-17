// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Utils;
using Aspire.V1;

namespace Aspire.Dashboard.Model;

internal static class ResourceServiceMessageExtensions
{
    public static ResourceViewModel ToViewModel(this Resource resource)
    {
        return new()
        {
            Name = resource.Name,
            ResourceType = resource.ResourceType,
            DisplayName = resource.DisplayName,
            Uid = resource.Uid,
            CreationTimeStamp = resource.CreatedAt.ToDateTime(),
            Properties = resource.Properties.ToFrozenDictionary(data => data.Name, data => data.Value, StringComparers.ResourceDataKey),
            Endpoints = GetEndpoints(),
            Environment = GetEnvironment(),
            ExpectedEndpointsCount = resource.ExpectedEndpointsCount,
            Services = GetServices(),
            State = resource.HasState ? resource.State : null,
        };

        ImmutableArray<ResourceServiceViewModel> GetServices()
        {
            return resource.Services
                .Select(s => new ResourceServiceViewModel(s.Name, s.AllocatedAddress, s.AllocatedPort))
                .ToImmutableArray();
        }

        ImmutableArray<EnvironmentVariableViewModel> GetEnvironment()
        {
            return resource.Environment
                .Select(s => new EnvironmentVariableViewModel(s.Name, s.Value, s.IsFromSpec))
                .ToImmutableArray();
        }

        ImmutableArray<EndpointViewModel> GetEndpoints()
        {
            return resource.Endpoints
                .Select(e => new EndpointViewModel(e.EndpointUrl, e.ProxyUrl))
                .ToImmutableArray();
        }
    }
}
