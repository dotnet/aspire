// Assembly 'Aspire.Oracle.EntityFrameworkCore'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Oracle.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring EntityFrameworkCore DbContext to Oracle database 
/// </summary>
public static class AspireOracleEFCoreExtensions
{
    /// <summary>
    /// Registers the given <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> as a service in the services provided by the <paramref name="builder" />.
    /// Enables db context pooling, retries, health check, logging and telemetry for the <see cref="T:Microsoft.EntityFrameworkCore.DbContext" />.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDbContextOptions">An optional delegate to configure the <see cref="T:Microsoft.EntityFrameworkCore.DbContextOptions" /> for the context.</param>
    /// <remarks>Reads the configuration from "Aspire:Oracle:EntityFrameworkCore:{typeof(TContext).Name}" config section, or "Aspire:Oracle:EntityFrameworkCore" if former does not exist.</remarks>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.Oracle.EntityFrameworkCore.OracleEntityFrameworkCoreSettings.ConnectionString" /> is not provided.</exception>
    public static void AddOracleDatabaseDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, Action<OracleEntityFrameworkCoreSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;

    /// <summary>
    /// Configures retries, health check, logging and telemetry for the <see cref="T:Microsoft.EntityFrameworkCore.DbContext" />.
    /// </summary>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> is not registered in DI.</exception>
    public static void EnrichOracleDatabaseDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<OracleEntityFrameworkCoreSettings>? configureSettings = null) where TContext : DbContext;
}
