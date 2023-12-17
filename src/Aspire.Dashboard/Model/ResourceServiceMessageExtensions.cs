// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
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
            ResourceType = resource.Id.ResourceType,
            DisplayName = resource.DisplayName,
            Uid = resource.Id.Uid,
            CreationTimeStamp = resource.CreatedAt.ToDateTime(),
            Properties = resource.Properties.ToFrozenDictionary(data => data.Name, data => data.Value, StringComparers.ResourceDataKey),
            Endpoints = [], // TODO
            Environment = [], // TODO
            ExpectedEndpointsCount = 0, // TODO
            Services = [], // TODO
            State = resource.HasState ? resource.State : null,
        };
    }
}
