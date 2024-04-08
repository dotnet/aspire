// Assembly 'Aspire.StackExchange.Redis'

using System.Runtime.CompilerServices;

namespace Aspire.StackExchange.Redis;

public sealed class StackExchangeRedisSettings
{
    public string? ConnectionString { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public StackExchangeRedisSettings();
}
