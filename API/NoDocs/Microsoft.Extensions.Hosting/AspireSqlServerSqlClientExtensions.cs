// Assembly 'Aspire.Microsoft.Data.SqlClient'

using System;
using Aspire.Microsoft.Data.SqlClient;

namespace Microsoft.Extensions.Hosting;

public static class AspireSqlServerSqlClientExtensions
{
    public static void AddSqlServerClient(this IHostApplicationBuilder builder, string connectionName, Action<MicrosoftDataSqlClientSettings>? configureSettings = null);
    public static void AddKeyedSqlServerClient(this IHostApplicationBuilder builder, string name, Action<MicrosoftDataSqlClientSettings>? configureSettings = null);
}
