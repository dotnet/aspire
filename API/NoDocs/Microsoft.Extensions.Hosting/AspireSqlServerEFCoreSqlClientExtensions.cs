// Assembly 'Aspire.Microsoft.EntityFrameworkCore.SqlServer'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

public static class AspireSqlServerEFCoreSqlClientExtensions
{
    public static void AddSqlServerDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;
    public static void EnrichSqlServerDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configureSettings = null) where TContext : DbContext;
}
