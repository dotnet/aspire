// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

/// <summary>
/// Represents an annotation for defining a script to create a database in PostgreSQL.
/// </summary>
internal sealed class PostgresCreateDatabaseScriptAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCreateDatabaseScriptAnnotation"/> class.
    /// </summary>
    /// <param name="script">The script used to create the database.</param>
    public PostgresCreateDatabaseScriptAnnotation(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        Script = script;
    }

    /// <summary>
    /// Gets the script used to create the database.
    /// </summary>
    public string Script { get; }
}
