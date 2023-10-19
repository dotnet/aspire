// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Data.Cosmos.EntityFrameworkCore.Tests;

public class ConformanceTests_NoPooling : ConformanceTests_Pooling
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Scoped;

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureDataCosmosEntityFrameworkCoreSettings>? configure = null, string? key = null)
    {
        builder.AddCosmosDBEntityFrameworkDBContext<TestDbContext>("cosmosdb", settings =>
        {
            settings.DbContextPooling = false;
            configure?.Invoke(settings);
        });
    }
}
