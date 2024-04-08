// Assembly 'Aspire.Pomelo.EntityFrameworkCore.MySql'

using System.Runtime.CompilerServices;

namespace Aspire.Pomelo.EntityFrameworkCore.MySql;

public sealed class PomeloEntityFrameworkCoreMySqlSettings
{
    public string? ConnectionString { get; set; }
    public string? ServerVersion { get; set; }
    public bool Retry { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public bool Metrics { get; set; }
    public int? CommandTimeout { get; set; }
    public PomeloEntityFrameworkCoreMySqlSettings();
}
