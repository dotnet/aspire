// Assembly 'Aspire.NATS.Net'

using System.Runtime.CompilerServices;

namespace Aspire.NATS.Net;

public sealed class NatsClientSettings
{
    public string? ConnectionString { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public NatsClientSettings();
}
