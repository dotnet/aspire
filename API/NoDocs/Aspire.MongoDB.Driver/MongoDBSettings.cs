// Assembly 'Aspire.MongoDB.Driver'

using System.Runtime.CompilerServices;

namespace Aspire.MongoDB.Driver;

public sealed class MongoDBSettings
{
    public string? ConnectionString { get; set; }
    public bool HealthChecks { get; set; }
    public int? HealthCheckTimeout { get; set; }
    public bool Tracing { get; set; }
    public MongoDBSettings();
}
