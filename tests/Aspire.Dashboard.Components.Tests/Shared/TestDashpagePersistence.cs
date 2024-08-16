// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components.Tests.Shared;

public sealed class TestDashpagePersistence : IDashpagePersistence
{
    public Task<ImmutableArray<DashpageDefinition>> GetDashpagesAsync(CancellationToken token)
    {
        return Task.FromResult<ImmutableArray<DashpageDefinition>>([]);
    }
}
