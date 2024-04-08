// Assembly 'Aspire.MySqlConnector'

using System.Runtime.CompilerServices;

namespace Aspire.MySqlConnector;

public sealed class MySqlConnectorSettings
{
    public string? ConnectionString { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public bool Metrics { get; set; }
    public MySqlConnectorSettings();
}
