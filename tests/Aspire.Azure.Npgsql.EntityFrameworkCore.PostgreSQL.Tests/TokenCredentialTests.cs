// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Npgsql.Tests;
using Aspire.TestUtilities;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class TokenCredentialTests
{
    // These tests use different connection strings to prevent efcore from reusing the data source results between tests.
    // c.f. https://github.com/npgsql/npgsql/issues/6085

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
        var connectionString = "Host=localhost;Database=test2";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", connectionString)
        ]);

        var fakeCred = new FakeTokenCredential(useManagedIdentity: false);

        if (useEnrich)
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(connectionString));
            builder.EnrichAzureNpgsqlDbContext<TestDbContext>(configureSettings: settings => settings.Credential = fakeCred);
        }
        else
        {
            builder.AddAzureNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting, configureSettings: settings => settings.Credential = fakeCred);
        }

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.Contains(connectionString, context.Database.GetDbConnection().ConnectionString);
        Assert.Contains("Username=mikey@mouse.com", context.Database.GetDbConnection().ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsUsernameFromManagedIdentityToken(bool useEnrich)
    {
        var connectionString = "Host=localhost;Database=test3";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", connectionString)
        ]);

        var fakeCred = new FakeTokenCredential(useManagedIdentity: true);

        if (useEnrich)
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(connectionString));
            builder.EnrichAzureNpgsqlDbContext<TestDbContext>(configureSettings: settings => settings.Credential = fakeCred);
        }
        else
        {
            builder.AddAzureNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting, configureSettings: settings => settings.Credential = fakeCred);
        }

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        Assert.Contains(connectionString, context.Database.GetDbConnection().ConnectionString);
        Assert.Contains("Username=mi-123", context.Database.GetDbConnection().ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TokenCredentialIsIgnoredWhenUsernameAndPasswordAreSet(bool useEnrich)
    {
        const string connectionString = "Host=localhost;Database=test;Username=admin;Password=p@ssw0rd1;Persist Security Info=True";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", connectionString)
        ]);

        var fakeCred = new FakeTokenCredential(useManagedIdentity: false);

        if (useEnrich)
        {
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
        Assert.Equal(connectionString, context.Database.GetDbConnection().ConnectionString);
        Assert.False(fakeCred.IsGetTokenInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesNotThrowWhenTokenCredentialHasNoUsername(bool useEnrich)
    {
        const string connectionString = "Host=localhost;Database=test4";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", connectionString)
        ]);

        var fakeCred = CreateAnonumousTokenCredentials();

        if (useEnrich)
        {
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
        Assert.Equal(connectionString, context.Database.GetDbConnection().ConnectionString);
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
