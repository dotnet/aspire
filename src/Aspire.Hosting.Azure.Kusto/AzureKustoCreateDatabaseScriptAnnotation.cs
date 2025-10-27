// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation to store a Kusto database creation script.
/// </summary>
internal sealed class AzureKustoCreateDatabaseScriptAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKustoCreateDatabaseScriptAnnotation"/> class.
    /// </summary>
    /// <param name="script">
    /// The Kusto script to create the database.
    /// </param>
    public AzureKustoCreateDatabaseScriptAnnotation(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        Script = script;
    }

    /// <summary>
    /// Gets the Kusto script to create the database.
    /// </summary>
    public string Script { get; }
}
