// Assembly 'Aspire.RabbitMQ.Client'

using System.Runtime.CompilerServices;

namespace Aspire.RabbitMQ.Client;

/// <summary>
/// Provides the client configuration settings for connecting to a RabbitMQ message broker.
/// </summary>
public sealed class RabbitMQClientSettings
{
    /// <summary>
    /// Gets or sets the connection string of the RabbitMQ server to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// <para>Gets or sets the maximum number of connection retry attempts.</para>
    /// <para>Default value is 5, set it to 0 to disable the retry mechanism.</para>
    /// </summary>
    public int MaxConnectRetryCount { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the RabbitMQ health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Tracing { get; set; }

    public RabbitMQClientSettings();
}
