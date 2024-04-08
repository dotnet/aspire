// Assembly 'Aspire.Oracle.EntityFrameworkCore'

using System.Runtime.CompilerServices;

namespace Aspire.Oracle.EntityFrameworkCore;

public sealed class OracleEntityFrameworkCoreSettings
{
    public string? ConnectionString { get; set; }
    public bool Retry { get; set; }
    public bool HealthChecks { get; set; }
    public int? CommandTimeout { get; set; }
    public OracleEntityFrameworkCoreSettings();
}
