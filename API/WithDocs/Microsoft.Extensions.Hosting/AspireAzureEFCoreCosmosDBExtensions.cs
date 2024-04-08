// Assembly 'Aspire.Microsoft.EntityFrameworkCore.Cosmos'

using System;
using System.Diagnostics.CodeAnalysis;
using Aspire.Microsoft.EntityFrameworkCore.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring EntityFrameworkCore DbContext to Azure Cosmos DB
/// </summary>
public static class AspireAzureEFCoreCosmosDBExtensions
{
    /// <summary>
    /// Registers the given <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> as a service in the services provided by the <paramref name="builder" />.
    /// Enables db context pooling, logging and telemetry.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="databaseName">The name of the database to use within the Azure Cosmos DB account.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing settings. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDbContextOptions">An optional delegate to configure the <see cref="T:Microsoft.EntityFrameworkCore.DbContextOptions" /> for the context.</param>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="P:Aspire.Microsoft.EntityFrameworkCore.Cosmos.EntityFrameworkCoreCosmosDBSettings.ConnectionString" /> is not provided.</exception>
    public static void AddCosmosDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, string connectionName, string databaseName, Action<EntityFrameworkCoreCosmosDBSettings>? configureSettings = null, Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext;

    /// <summary>
    /// Configures logging and telemetry for the <see cref="T:Microsoft.EntityFrameworkCore.DbContext" />.
    /// </summary>
    /// <exception cref="T:System.ArgumentNullException">Thrown if mandatory <paramref name="builder" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">Thrown when mandatory <see cref="T:Microsoft.EntityFrameworkCore.DbContext" /> is not registered in DI.</exception>
    public static void EnrichCosmosDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(this IHostApplicationBuilder builder, Action<EntityFrameworkCoreCosmosDBSettings>? configureSettings = null) where TContext : DbContext;
}
