// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;

namespace Aspire.V1;

partial class Resource
{
    /// <summary>
    /// Converts this gRPC message object to a view model for use in the dashboard UI.
    /// </summary>
    public ResourceViewModel ToViewModel()
    {
        return new()
        {
            Name = Name,
            ResourceType = ResourceType,
            DisplayName = DisplayName,
            Uid = Uid,
            CreationTimeStamp = CreatedAt.ToDateTime(),
            Properties = Properties.ToFrozenDictionary(p => p.Name, p => p.Value, StringComparers.ResourcePropertyName),
            Endpoints = GetEndpoints(),
            Environment = GetEnvironment(),
            ExpectedEndpointsCount = ExpectedEndpointsCount,
            Services = GetServices(),
            State = HasState ? State : null
        };

        ImmutableArray<ResourceServiceViewModel> GetServices()
        {
            return Services
                .Select(s => new ResourceServiceViewModel(s.Name, s.AllocatedAddress, s.AllocatedPort))
                .ToImmutableArray();
        }

        ImmutableArray<EnvironmentVariableViewModel> GetEnvironment()
        {
            return Environment
                .Select(s => new EnvironmentVariableViewModel(s.Name, s.Value, s.IsFromSpec))
                .ToImmutableArray();
        }

        ImmutableArray<EndpointViewModel> GetEndpoints()
        {
            return Endpoints
                .Select(e => new EndpointViewModel(e.EndpointUrl, e.ProxyUrl))
                .ToImmutableArray();
        }
    }
}
