// Assembly 'Aspire.Npgsql.EntityFrameworkCore.PostgreSQL'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

public static class AspireEFPostgreSqlExtensions
{
    public static void AddNpgsqlDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;
    public static void EnrichNpgsqlDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null) where TContext : DbContext;
}
