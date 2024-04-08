// Assembly 'Aspire.Microsoft.Data.SqlClient'

using System.Runtime.CompilerServices;

namespace Aspire.Microsoft.Data.SqlClient;

public sealed class MicrosoftDataSqlClientSettings
{
    public string? ConnectionString { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public MicrosoftDataSqlClientSettings();
}
