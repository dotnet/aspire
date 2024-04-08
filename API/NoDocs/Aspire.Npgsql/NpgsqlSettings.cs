// Assembly 'Aspire.Npgsql'

using System.Runtime.CompilerServices;

namespace Aspire.Npgsql;

public sealed class NpgsqlSettings
{
    public string? ConnectionString { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public bool Metrics { get; set; }
    public NpgsqlSettings();
}
