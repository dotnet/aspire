// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable disable

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.SqlClient.Implementation;

/// <summary>
/// Helper class to hold common properties used by both SqlClientDiagnosticListener on .NET Core
/// and SqlEventSourceListener on .NET Framework.
/// </summary>
internal sealed class SqlActivitySourceHelper
{
    public const string MicrosoftSqlServerDatabaseSystemName = "mssql";

    public static readonly AssemblyName AssemblyName = typeof(SqlActivitySourceHelper).Assembly.GetName();
    public static readonly string ActivitySourceName = AssemblyName.Name;
    public static readonly Version Version = AssemblyName.Version;
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());
    public static readonly string ActivityName = ActivitySourceName + ".Execute";

    public static readonly IEnumerable<KeyValuePair<string, object>> CreationTags = new[]
    {
        new KeyValuePair<string, object>(SemanticConventions.AttributeDbSystem, MicrosoftSqlServerDatabaseSystemName),
    };
}
