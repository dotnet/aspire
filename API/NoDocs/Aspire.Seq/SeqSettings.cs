// Assembly 'Aspire.Seq'

using System.Runtime.CompilerServices;

namespace Aspire.Seq;

public sealed class SeqSettings
{
    public bool HealthChecks { get; set; }
    public string? ApiKey { get; set; }
    public string? ServerUrl { get; set; }
    public SeqSettings();
}
