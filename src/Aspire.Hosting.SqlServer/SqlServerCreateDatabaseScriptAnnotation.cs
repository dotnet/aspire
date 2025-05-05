// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an annotation for defining a script to create a database in SQL Server.
/// </summary>
internal sealed class SqlServerCreateDatabaseScriptAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerCreateDatabaseScriptAnnotation"/> class.
    /// </summary>
    /// <param name="script">The script used to create the database.</param>
    public SqlServerCreateDatabaseScriptAnnotation(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        Script = script;
    }

    /// <summary>
    /// Gets the script used to create the database.
    /// </summary>
    public string Script { get; }
}
