// Assembly 'Aspire.Npgsql.EntityFrameworkCore.PostgreSQL'

using System.Runtime.CompilerServices;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;

public sealed class NpgsqlEntityFrameworkCorePostgreSQLSettings
{
    public string? ConnectionString { get; set; }
    public bool Retry { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public bool Metrics { get; set; }
    public int? CommandTimeout { get; set; }
    public NpgsqlEntityFrameworkCorePostgreSQLSettings();
}
