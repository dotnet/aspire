// Assembly 'Aspire.Pomelo.EntityFrameworkCore.MySql'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering a MySQL database context in an Aspire application.
/// </summary>
public static class AspireEFMySqlExtensions
{
    /// <summary>
    /// Registers the given <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> as a service in the services provided by the <paramref name="builder" />.
    /// Enables db context pooling, retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDbContextOptions">An optional delegate to configure the <see cref="T:Microsoft.EntityFrameworkCore.DbContextOptions" /> for the context.</param>
    /// <remarks>
    /// <para>
    /// Reads the configuration from "Aspire:Pomelo:EntityFrameworkCore:MySql:{typeof(TContext).Name}" config section, or "Aspire:Pomelo:EntityFrameworkCore:MySql" if former does not exist.
    /// </para>
    /// <para>
    /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method can then be overridden to configure <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> options.
    /// </para>
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.Pomelo.EntityFrameworkCore.MySql.PomeloEntityFrameworkCoreMySqlSettings.ConnectionString" /> is not provided.</exception>
    public static void AddMySqlDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, Action<PomeloEntityFrameworkCoreMySqlSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;

    /// <summary>
    /// Configures retries, health check, logging and telemetry for the <see cref="T:Microsoft.EntityFrameworkCore.DbContext" />.
    /// </summary>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> is not registered in DI.</exception>
    public static void EnrichMySqlDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<PomeloEntityFrameworkCoreMySqlSettings>? configureSettings = null) where TContext : DbContext;
}
