// Assembly 'Aspire.Npgsql'

using System;
using Aspire.Npgsql;
using Npgsql;

namespace Microsoft.Extensions.Hosting;

public static class AspirePostgreSqlNpgsqlExtensions
{
    public static void AddNpgsqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null);
    public static void AddKeyedNpgsqlDataSource(this IHostApplicationBuilder builder, string name, Action<NpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null);
}
