// Assembly 'Aspire.Microsoft.EntityFrameworkCore.Cosmos'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Microsoft.EntityFrameworkCore.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

public static class AspireAzureEFCoreCosmosDBExtensions
{
    public static void AddCosmosDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, string databaseName, Action<EntityFrameworkCoreCosmosDBSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;
    public static void EnrichCosmosDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<EntityFrameworkCoreCosmosDBSettings>? configureSettings = null) where TContext : DbContext;
}
