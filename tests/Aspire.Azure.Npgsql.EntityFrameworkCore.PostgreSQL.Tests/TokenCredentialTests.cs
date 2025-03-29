// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class TokenCredentialTests
{
    private const string ConnectionString = "Host=localhost;Database=test";
    private const string ConnectionStringWithUsernameAndPassword = "Host=localhost;Database=test;Username=admin;Password=p@ssw0rd1";

    internal static void ConfigureDbContextOptionsBuilderForTesting(DbContextOptionsBuilder builder)
    {
        // Don't cache the service provider in testing.
        // Works around https://github.com/npgsql/efcore.pg/issues/2891, which is errantly caches connection strings across DI containers.
        builder.EnableServiceProviderCaching(false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsUsernameFromToken(bool useEnrich)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        var fakeCred = new FakeTokenCredential(useManagedIdentity: false);

        if (useEnrich)
        {
            var connectionString = builder.Configuration.GetConnectionString("npgsql");
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(connectionString));
            builder.EnrichAzureNpgsqlDbContext<TestDbContext>(configureSettings: settings => settings.Credential = fakeCred);
        }
        else
        {
            builder.AddAzureNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting, configureSettings: settings => settings.Credential = fakeCred);
        }

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.Contains(ConnectionString, context.Database.GetDbConnection().ConnectionString);
        Assert.Contains("Username=mikey@mouse.com", context.Database.GetDbConnection().ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsUsernameFromManagedIdentityToken(bool useEnrich)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        var fakeCred = new FakeTokenCredential(useManagedIdentity: true);

        if (useEnrich)
        {
            var connectionString = builder.Configuration.GetConnectionString("npgsql");
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(connectionString));
            builder.EnrichAzureNpgsqlDbContext<TestDbContext>(configureSettings: settings => settings.Credential = fakeCred);
        }
        else
        {
            builder.AddAzureNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting, configureSettings: settings => settings.Credential = fakeCred);
        }

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.Contains(ConnectionString, context.Database.GetDbConnection().ConnectionString);
        Assert.Contains("Username=mi-123", context.Database.GetDbConnection().ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TokenCredentialIsIgnoredWhenUsernameAndPasswordAreSet(bool useEnrich)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionStringWithUsernameAndPassword)
        ]);

        var fakeCred = new FakeTokenCredential(useManagedIdentity: false);

        if (useEnrich)
        {
            var connectionString = builder.Configuration.GetConnectionString("npgsql");
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(connectionString));
            builder.EnrichAzureNpgsqlDbContext<TestDbContext>(configureSettings: settings => settings.Credential = fakeCred);
        }
        else
        {
            builder.AddAzureNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting, configureSettings: settings => settings.Credential = fakeCred);
        }

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.NotNull(fakeCred);
        Assert.Equal(ConnectionStringWithUsernameAndPassword, context.Database.GetDbConnection().ConnectionString);
        Assert.False(fakeCred.IsGetTokenInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesNotThrowWhenTokenCredentialHasNoUsername(bool useEnrich)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        var fakeCred = CreateAnonumousTokenCredentials();

        if (useEnrich)
        {
            var connectionString = builder.Configuration.GetConnectionString("npgsql");
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(connectionString));
            builder.EnrichAzureNpgsqlDbContext<TestDbContext>(configureSettings: settings => settings.Credential = fakeCred);
        }
        else
        {
            builder.AddAzureNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting, configureSettings: settings => settings.Credential = fakeCred);
        }

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.NotNull(fakeCred);
        Assert.Equal(ConnectionString, context.Database.GetDbConnection().ConnectionString);
        Assert.True(fakeCred.IsGetTokenInvoked);
        Assert.Contains("https://ossrdbms-aad.database.windows.net/.default", fakeCred.RequestedScopes);
        Assert.Contains("https://management.azure.com/.default", fakeCred.RequestedScopes);
    }

    private static FakeTokenCredential CreateAnonumousTokenCredentials()
    {
        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJJc3N1ZXIiOiJJc3N1ZXIiLCJJc3N1ZWQgQXQiOiIyMDI1LTAzLTIxVDAxOjM3OjAwLjE5OFoiLCJFeHBpcmF0aW9uIjoiMjAyNS0wMy0yMVQwMTozNzowMC4xOThaIiwiUm9sZSI6IkFkbWluIn0.nT9VhsXfI0v78C5J57ehy3NERNNN0e6NvVZwq_XOr-A";
        var accesstoken = new AccessToken(token, DateTimeOffset.Now.AddHours(1));

        // {
        //   "Issuer": "Issuer",
        //   "Issued At": "2025-03-21T01:37:00.198Z",
        //   "Expiration": "2025-03-21T01:37:00.198Z",
        //   "Role": "Admin"
        // }

        return new FakeTokenCredential(accesstoken);
    }
}
