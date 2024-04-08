// Assembly 'Aspire.MySqlConnector'

using System;
using Aspire.MySqlConnector;

namespace Microsoft.Extensions.Hosting;

public static class AspireMySqlConnectorExtensions
{
    public static void AddMySqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<MySqlConnectorSettings>? configureSettings = null);
    public static void AddKeyedMySqlDataSource(this IHostApplicationBuilder builder, string name, Action<MySqlConnectorSettings>? configureSettings = null);
}
