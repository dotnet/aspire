// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Kusto;

/// <summary>
/// Annotation to store a Kusto table or creation script.
/// </summary>
internal sealed class KustoCreationScriptAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KustoCreationScriptAnnotation"/> class.
    /// </summary>
    /// <param name="script">
    /// The Kusto script to create the database, table, or data.
    /// </param>
    /// <param name="database">
    /// The name of the database where the table will be created. If <c langword="null"/>, the default database will be used.
    /// </param>
    public KustoCreationScriptAnnotation(string script, string? database)
    {
        Script = script;
        Database = database;
    }

    /// <summary>
    /// Gets the Kusto script to create the database, table, or data.
    /// </summary>
    public string Script { get; }

    /// <summary>
    /// Gets the name of the database where the table will be created. If <c langword="null"/>, the default database will be used.
    /// </summary>
    public string? Database { get; }
}
