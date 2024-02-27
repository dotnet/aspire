// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Xunit;

namespace Aspire.Pomelo.EntityFrameworkCore.MySql.Tests;

public class AspireEFMySqlExtensionsTests
{
    private const string ConnectionString = "Server=localhost;User ID=root;Database=test";
    private const string ConnectionStringSuffixAddedByPomelo = ";Allow User Variables=True;Use Affected Rows=False";

    [Fact]
    public void ReadsFromConnectionStringsCorrectly()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ServerVersion", "8.2.0-mysql"),
            new KeyValuePair<string, string?>("ConnectionStrings:mysql", ConnectionString)
        ]);

        builder.AddMySqlDbContext<TestDbContext>("mysql");

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.Equal(ConnectionString + ConnectionStringSuffixAddedByPomelo, context.Database.GetDbConnection().ConnectionString);
    }

    [Fact]
    public void ConnectionStringCanBeSetInCode()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ServerVersion", "8.2.0-mysql"),
            new KeyValuePair<string, string?>("ConnectionStrings:mysql", "unused")
        ]);

        builder.AddMySqlDbContext<TestDbContext>("mysql", settings => settings.ConnectionString = ConnectionString);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString + ConnectionStringSuffixAddedByPomelo, actualConnectionString);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", actualConnectionString);
    }

    [Fact]
    public void ConnectionNameWinsOverConfigSection()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ServerVersion", "8.2.0-mysql"),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ConnectionString", "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:mysql", ConnectionString)
        ]);

        builder.AddMySqlDbContext<TestDbContext>("mysql");

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString + ConnectionStringSuffixAddedByPomelo, actualConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", actualConnectionString);
    }

    [Fact]
    public void CanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ServerVersion", "8.2.0-mysql"),
            new KeyValuePair<string, string?>("ConnectionStrings:mysql", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:Retry", "true")
        ]);

        builder.AddMySqlDbContext<TestDbContext>("mysql", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseMySql(new MySqlServerVersion(new Version(8, 2, 0)), mySqlBuilder =>
            {
                mySqlBuilder.CommandTimeout(123);
            });
        });

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);

        // ensure the connection string from config was respected
        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString + ";Allow User Variables=True;Default Command Timeout=123;Use Affected Rows=False", actualConnectionString);

        // ensure the retry strategy is enabled and set to its default value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<MySqlRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void CanConfigureDbContextOptionsWithoutRetry()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ServerVersion", "8.2.0-mysql"),
            new KeyValuePair<string, string?>("ConnectionStrings:mysql", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:Retry", "false")
        ]);

        builder.AddMySqlDbContext<TestDbContext>("mysql", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseMySql(new MySqlServerVersion(new Version(8, 2, 0)), mySqlBuilder =>
            {
                mySqlBuilder.CommandTimeout(123);
            });
        });

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);

        // ensure the connection string from config was respected
        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString + ";Allow User Variables=True;Default Command Timeout=123;Use Affected Rows=False", actualConnectionString);

        // ensure no retry strategy was registered
        Assert.Null(extension.ExecutionStrategyFactory);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThrowsWhenDbContextIsRegisteredBeforeAspireComponent(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mysql", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 2, 0))));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 2, 0))));
        }

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddMySqlDbContext<TestDbContext>("mysql"));
        Assert.Equal("DbContext<TestDbContext> is already registered. Please ensure 'services.AddDbContext<TestDbContext>()' is not used when calling 'AddMySqlDbContext()' or use the corresponding 'Enrich' method.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesntThrowWhenDbContextIsRegisteredBeforeAspireComponentProduction(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mysql", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 2, 0))));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 2, 0))));
        }

        var exception = Record.Exception(() => builder.AddMySqlDbContext<TestDbContext>("mysql"));

        Assert.Null(exception);
    }
}
