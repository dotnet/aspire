// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Storage;

/// <summary>
/// Configuration options for telemetry storage.
/// </summary>
public sealed class TelemetryStorageOptions
{
    /// <summary>
    /// Gets or sets the type of storage provider to use.
    /// </summary>
    public StorageProviderType ProviderType { get; set; } = StorageProviderType.InMemory;

    /// <summary>
    /// Gets or sets the connection string or file path for the storage provider.
    /// For SQLite, this is the path to the database file.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically create the database if it doesn't exist (SQLite only).
    /// </summary>
    public bool AutoCreateDatabase { get; set; } = true;
}
