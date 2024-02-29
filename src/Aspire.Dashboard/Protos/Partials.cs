// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Aspire.Dashboard.Model;

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
            Name = ValidateNotNull(Name),
            ResourceType = ValidateNotNull(ResourceType),
            DisplayName = ValidateNotNull(DisplayName),
            Uid = ValidateNotNull(Uid),
            CreationTimeStamp = CreatedAt.ToDateTime(),
            Properties = Properties.ToFrozenDictionary(property => ValidateNotNull(property.Name), property => ValidateNotNull(property.Value), StringComparers.ResourcePropertyName),
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
                .Select(e => new EnvironmentVariableViewModel(e.Name, e.Value, e.IsFromSpec))
                .ToImmutableArray();
        }

        ImmutableArray<EndpointViewModel> GetEndpoints()
        {
            return Endpoints
                .Select(e => new EndpointViewModel(e.EndpointUrl, e.ProxyUrl))
                .ToImmutableArray();
        }

        T ValidateNotNull<T>(T value, [CallerArgumentExpression(nameof(value))] string? expression = null) where T : class
        {
            if (value is null)
            {
                throw new InvalidOperationException($"Message field '{expression}' on resource with name '{Name}' cannot be null.");
            }

            return value;
        }
    }
}
