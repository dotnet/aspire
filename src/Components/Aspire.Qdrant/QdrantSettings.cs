// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Qdrant;
public sealed class QdrantSettings
{
    /// <summary>
    /// The connection string of the Qdrant server to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The API Key of the Qdrant server to connect to.
    /// </summary>
    public string? ApiKey { get; set; }
}
