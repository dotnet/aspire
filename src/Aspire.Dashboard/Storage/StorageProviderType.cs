// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Storage;

/// <summary>
/// Defines the type of storage provider to use for telemetry data.
/// </summary>
public enum StorageProviderType
{
    /// <summary>
    /// In-memory storage (default). Data is lost when the application restarts.
    /// </summary>
    InMemory,

    /// <summary>
    /// SQLite database storage. Data persists to disk.
    /// </summary>
    SQLite
}
