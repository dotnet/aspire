// Assembly 'Aspire.Oracle.EntityFrameworkCore'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Oracle.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

public static class AspireOracleEFCoreExtensions
{
    public static void AddOracleDatabaseDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, Action<OracleEntityFrameworkCoreSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;
    public static void EnrichOracleDatabaseDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<OracleEntityFrameworkCoreSettings>? configureSettings = null) where TContext : DbContext;
}
