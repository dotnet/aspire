// Assembly 'Aspire.RabbitMQ.Client'

using System.Runtime.CompilerServices;

namespace Aspire.RabbitMQ.Client;

public sealed class RabbitMQClientSettings
{
    public string? ConnectionString { get; set; }
    public int MaxConnectRetryCount { get; set; }
    public bool HealthChecks { get; set; }
    public bool Tracing { get; set; }
    public RabbitMQClientSettings();
}
