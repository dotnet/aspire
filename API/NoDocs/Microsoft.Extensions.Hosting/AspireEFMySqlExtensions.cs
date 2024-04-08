// Assembly 'Aspire.Pomelo.EntityFrameworkCore.MySql'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

public static class AspireEFMySqlExtensions
{
    public static void AddMySqlDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, Action<PomeloEntityFrameworkCoreMySqlSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;
    public static void EnrichMySqlDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<PomeloEntityFrameworkCoreMySqlSettings>? configureSettings = null) where TContext : DbContext;
}
