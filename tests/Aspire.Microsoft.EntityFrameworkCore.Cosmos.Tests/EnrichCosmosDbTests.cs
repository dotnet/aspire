// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

public class EnrichCosmosDbTests : ConformanceTests
{
    private const string ConnectionString = "Host=fake;Database=catalog";
    private const string DatabaseName = "TestDatabase";

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<EntityFrameworkCoreCosmosDBSettings>? configure = null, string? key = null)
    {
        builder.Services.AddDbContextPool<TestDbContext>(options => options.UseCosmos(ConnectionString, DatabaseName));
        builder.EnrichCosmosDbContext<TestDbContext>(configure);
    }

    [Fact]
    public void ShouldThrowIfDbContextIsNotRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.EnrichCosmosDbContext<TestDbContext>());
        Assert.Contains("DbContext<TestDbContext> was not registered", exception.Message);
    }

    [Fact]
    public void ShouldNotThrowIfDbContextIsRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddDbContext<TestDbContext>(options => options.UseCosmos(ConnectionString, DatabaseName));

        builder.EnrichCosmosDbContext<TestDbContext>();
    }

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        throw new SkipTestException("Enrich doesn't use ConnectionString");
    }

    [Fact]
    public void EnrichCanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseCosmos(ConnectionString, DatabaseName, builder =>
            {
                builder.RequestTimeout(TimeSpan.FromSeconds(123));
                builder.Region("westus");
            });
        });

        builder.EnrichCosmosDbContext<TestDbContext>();

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<CosmosOptionsExtension>();
        Assert.NotNull(extension);

        // Ensure the request timeout was respected
        Assert.Equal(TimeSpan.FromSeconds(123), extension.RequestTimeout);

        // Ensure the region from the lambda was respected
        Assert.Equal("westus", extension.Region);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichSupportServiceType()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseCosmos(ConnectionString, DatabaseName);
        });

        builder.EnrichCosmosDbContext<TestDbContext>();

        var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);
    }

    [Fact]
    public void EnrichSupportCustomOptionsLifetime()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContext<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseCosmos(ConnectionString, DatabaseName);
        }, contextLifetime: ServiceLifetime.Singleton);

        builder.EnrichCosmosDbContext<TestDbContext>();

        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(optionsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);
    }
}
