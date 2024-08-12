// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Eventing;

public class DistributedApplicationBuilderEventingTests
{
    [Fact]
    public void CanResolveIDistributedApplicationEventingFromDI()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        using var app = builder.Build();
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        Assert.Equal(builder.Eventing, eventing);
    }
}
