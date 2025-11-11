// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data.Common;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Default values for the Kusto emulator container.
/// </summary>
internal static class AzureKustoEmulatorContainerDefaults
{
    /// <summary>
    /// The default target port for the Kusto emulator container query endpoint.
    /// Based on Azure Data Explorer emulator documentation, it typically uses port 8080.
    /// </summary>
    public const int DefaultTargetPort = 8080;

    /// <summary>
    /// The default (emulator local) path used for persisting Kusto databases. This path
    /// can be mounted as a volume to persist database data across container restarts.
    /// </summary>
    /// <remarks>/kustodata/dbs/</remarks>
    public const string DefaultPersistencePath = "/kustodata/dbs/";

    public static string DefaultCreateDatabaseCommand(string dbName, string persistencePathRoot = DefaultPersistencePath)
    {
        var root = persistencePathRoot.AsSpan().TrimEnd('/');

        return CslCommandGenerator.GenerateDatabaseCreateCommand(
            dbName,
            metadataPersistentPath: $"{root}/{dbName}/md",
            dataPersistentPath: $"{root}/{dbName}/data",
            ifNotExists: true);
    }
}
