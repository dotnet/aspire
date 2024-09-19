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
internal static class SqlActivitySourceHelper
{
    public const string MicrosoftSqlServerDatabaseSystemName = "mssql";

    public const string ActivitySourceName = "OpenTelemetry.Instrumentation.SqlClient";
    public static readonly Version Version = new Version(1, 7, 0, 1173);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());
    public const string ActivityName = ActivitySourceName + ".Execute";

    public static readonly IEnumerable<KeyValuePair<string, object>> CreationTags = new[]
    {
        new KeyValuePair<string, object>(SemanticConventions.AttributeDbSystem, MicrosoftSqlServerDatabaseSystemName),
    };
}
