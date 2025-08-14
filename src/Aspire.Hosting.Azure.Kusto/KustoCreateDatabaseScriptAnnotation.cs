// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Annotation to store a Kusto database creation script.
/// </summary>
internal sealed class KustoCreateDatabaseScriptAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KustoCreateDatabaseScriptAnnotation"/> class.
    /// </summary>
    /// <param name="script">
    /// The Kusto script to create the database.
    /// </param>
    public KustoCreateDatabaseScriptAnnotation(string script)
    {
        Script = script;
    }

    /// <summary>
    /// Gets the Kusto script to create the database.
    /// </summary>
    public string Script { get; }
}
