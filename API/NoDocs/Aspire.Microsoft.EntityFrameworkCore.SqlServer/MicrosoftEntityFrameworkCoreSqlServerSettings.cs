// Assembly 'Aspire.Microsoft.EntityFrameworkCore.SqlServer'

using System.Runtime.CompilerServices;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer;

public sealed class MicrosoftEntityFrameworkCoreSqlServerSettings
{
    public string? ConnectionString { get; set; }
    public bool Retry { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public int? CommandTimeout { get; set; }
    public MicrosoftEntityFrameworkCoreSqlServerSettings();
}
