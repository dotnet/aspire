// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer.Tests;

public class ConformanceTests_NoPooling : ConformanceTests_Pooling
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Scoped;

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configure = null, string? key = null)
    {
        builder.AddSqlServerDbContext<TestDbContext>("sqlconnection", settings =>
        {
            settings.DbContextPooling = false;
            configure?.Invoke(settings);
        });
    }
}
