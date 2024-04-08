// Assembly 'Aspire.MongoDB.Driver'

using System.Runtime.CompilerServices;

namespace Aspire.MongoDB.Driver;

/// <summary>
/// Provides the client configuration settings for connecting to a MongoDB database using MongoDB driver.
/// </summary>
public sealed class MongoDBSettings
{
    /// <summary>
    /// Gets or sets the connection string of the MongoDB database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the MongoDB health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a integer value that indicates the MongoDB health check timeout in milliseconds.
    /// </summary>
    public int? HealthCheckTimeout { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Tracing { get; set; }

    public MongoDBSettings();
}
