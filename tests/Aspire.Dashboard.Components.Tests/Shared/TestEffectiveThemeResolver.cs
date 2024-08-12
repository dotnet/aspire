// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components.Tests.Shared;

public sealed class TestEffectiveThemeResolver : IEffectiveThemeResolver
{
    public Task<string> GetEffectiveThemeAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult("Dark");
    }
}
