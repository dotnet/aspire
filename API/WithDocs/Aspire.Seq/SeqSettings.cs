// Assembly 'Aspire.Seq'

using System.Runtime.CompilerServices;

namespace Aspire.Seq;

/// <summary>
/// Provides the client configuration settings for connecting telemetry to a Seq server.
/// </summary>
public sealed class SeqSettings
{
    /// <summary>
    /// Gets or sets a boolean value that indicates whetherthe Seq server health check is enabled or not.
    /// </summary>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a Seq <i>API key</i> that authenticates the client to the Seq server.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the base URL of the Seq server (including protocol and port). E.g. "https://example.seq.com:6789"
    /// </summary>
    public string? ServerUrl { get; set; }

    public SeqSettings();
}
