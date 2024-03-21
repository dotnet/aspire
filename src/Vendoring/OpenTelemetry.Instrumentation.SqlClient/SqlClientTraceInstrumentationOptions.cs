// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#nullable disable

using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.SqlClient;

/// <summary>
/// Options for <see cref="SqlClientInstrumentation"/>.
/// </summary>
/// <remarks>
/// For help and examples see: <a href="https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.SqlClient/README.md#advanced-configuration" />.
/// </remarks>
internal class SqlClientTraceInstrumentationOptions
{
    /*
     * Match...
     *  protocol[ ]:[ ]serverName
     *  serverName
     *  serverName[ ]\[ ]instanceName
     *  serverName[ ],[ ]port
     *  serverName[ ]\[ ]instanceName[ ],[ ]port
     *
     * [ ] can be any number of white-space, SQL allows it for some reason.
     *
     * Optional "protocol" can be "tcp", "lpc" (shared memory), or "np" (named pipes). See:
     *  https://docs.microsoft.com/troubleshoot/sql/connect/use-server-name-parameter-connection-string, and
     *  https://docs.microsoft.com/dotnet/api/system.data.sqlclient.sqlconnection.connectionstring?view=dotnet-plat-ext-5.0
     *
     * In case of named pipes the Data Source string can take form of:
     *  np:serverName\instanceName, or
     *  np:\\serverName\pipe\pipeName, or
     *  np:\\serverName\pipe\MSSQL$instanceName\pipeName - in this case a separate regex (see NamedPipeRegex below)
     *  is used to extract instanceName
     */
    private static readonly Regex DataSourceRegex = new("^(.*\\s*:\\s*\\\\{0,2})?(.*?)\\s*(?:[\\\\,]|$)\\s*(.*?)\\s*(?:,|$)\\s*(.*)$", RegexOptions.Compiled);

    /// <summary>
    /// In a Data Source string like "np:\\serverName\pipe\MSSQL$instanceName\pipeName" match the
    /// "pipe\MSSQL$instanceName" segment to extract instanceName if it is available.
    /// </summary>
    /// <see>
    /// <a href="https://docs.microsoft.com/previous-versions/sql/sql-server-2016/ms189307(v=sql.130)"/>
    /// </see>
    private static readonly Regex NamedPipeRegex = new("pipe\\\\MSSQL\\$(.*?)\\\\", RegexOptions.Compiled);

    private static readonly ConcurrentDictionary<string, SqlConnectionDetails> ConnectionDetailCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether or not the <see
    /// cref="SqlClientInstrumentation"/> should add the names of <see
    /// cref="CommandType.StoredProcedure"/> commands as the <see
    /// cref="SemanticConventions.AttributeDbStatement"/> tag. Default
    /// value: <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>SetDbStatementForStoredProcedure is only supported on .NET
    /// and .NET Core runtimes.</b></para>
    /// </remarks>
    public bool SetDbStatementForStoredProcedure { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not the <see
    /// cref="SqlClientInstrumentation"/> should add the text of commands as
    /// the <see cref="SemanticConventions.AttributeDbStatement"/> tag.
    /// Default value: <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WARNING: SetDbStatementForText will capture the raw
    /// <c>CommandText</c>. Make sure your <c>CommandText</c> property never
    /// contains any sensitive data.</b>
    /// </para>
    /// <para><b>SetDbStatementForText is supported on all runtimes.</b>
    /// <list type="bullet">
    /// <item>On .NET and .NET Core SetDbStatementForText only applies to
    /// <c>SqlCommand</c>s with <see cref="CommandType.Text"/>.</item>
    /// <item>On .NET Framework SetDbStatementForText applies to all
    /// <c>SqlCommand</c>s regardless of <see cref="CommandType"/>.
    /// <list type="bullet">
    /// <item>When using <c>System.Data.SqlClient</c> use
    /// SetDbStatementForText to capture StoredProcedure command
    /// names.</item>
    /// <item>When using <c>Microsoft.Data.SqlClient</c> use
    /// SetDbStatementForText to capture Text, StoredProcedure, and all
    /// other command text.</item>
    /// </list></item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool SetDbStatementForText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the <see
    /// cref="SqlClientInstrumentation"/> should parse the DataSource on a
    /// SqlConnection into server name, instance name, and/or port
    /// connection-level attribute tags. Default value: <see
    /// langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>EnableConnectionLevelAttributes is supported on all runtimes.</b>
    /// </para>
    /// <para>
    /// The default behavior is to set the SqlConnection DataSource as the <see cref="SemanticConventions.AttributePeerService"/> tag.
    /// If enabled, SqlConnection DataSource will be parsed and the server name will be sent as the
    /// <see cref="SemanticConventions.AttributeServerAddress"/> or <see cref="SemanticConventions.AttributeServerSocketAddress"/> tag,
    /// the instance name will be sent as the <see cref="SemanticConventions.AttributeDbMsSqlInstanceName"/> tag,
    /// and the port will be sent as the <see cref="SemanticConventions.AttributeServerPort"/> tag if it is not 1433 (the default port).
    /// </para>
    /// </remarks>
    public bool EnableConnectionLevelAttributes { get; set; }

    /// <summary>
    /// Gets or sets an action to enrich an <see cref="Activity"/> with the
    /// raw <c>SqlCommand</c> object.
    /// </summary>
    /// <remarks>
    /// <para><b>Enrich is only executed on .NET and .NET Core
    /// runtimes.</b></para>
    /// The parameters passed to the enrich action are:
    /// <list type="number">
    /// <item>The <see cref="Activity"/> being enriched.</item>
    /// <item>The name of the event. Currently only <c>"OnCustom"</c> is
    /// used but more events may be added in the future.</item>
    /// <item>The raw <c>SqlCommand</c> object from which additional
    /// information can be extracted to enrich the <see
    /// cref="Activity"/>.</item>
    /// </list>
    /// </remarks>
    public Action<Activity, string, object> Enrich { get; set; }

    /// <summary>
    /// Gets or sets a filter function that determines whether or not to
    /// collect telemetry about a command.
    /// </summary>
    /// <remarks>
    /// <para><b>Filter is only executed on .NET and .NET Core
    /// runtimes.</b></para>
    /// Notes:
    /// <list type="bullet">
    /// <item>The first parameter passed to the filter function is the raw
    /// <c>SqlCommand</c> object for the command being executed.</item>
    /// <item>The return value for the filter function is interpreted as:
    /// <list type="bullet">
    /// <item>If filter returns <see langword="true" />, the command is
    /// collected.</item>
    /// <item>If filter returns <see langword="false" /> or throws an
    /// exception the command is NOT collected.</item>
    /// </list></item>
    /// </list>
    /// </remarks>
    public Func<object, bool> Filter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the exception will be
    /// recorded as <see cref="ActivityEvent"/> or not. Default value: <see
    /// langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>RecordException is only supported on .NET and .NET Core
    /// runtimes.</b></para>
    /// <para>For specification details see: <see
    /// href="https://github.com/open-telemetry/semantic-conventions/blob/main/docs/exceptions/exceptions-spans.md"/>.</para>
    /// </remarks>
    public bool RecordException { get; set; }

    internal static SqlConnectionDetails ParseDataSource(string dataSource)
    {
        Match match = DataSourceRegex.Match(dataSource);

        string serverHostName = match.Groups[2].Value;
        string serverIpAddress = null;

        string instanceName;

        var uriHostNameType = Uri.CheckHostName(serverHostName);
        if (uriHostNameType == UriHostNameType.IPv4 || uriHostNameType == UriHostNameType.IPv6)
        {
            serverIpAddress = serverHostName;
            serverHostName = null;
        }

        string maybeProtocol = match.Groups[1].Value;
        bool isNamedPipe = maybeProtocol.Length > 0 &&
                           maybeProtocol.StartsWith("np", StringComparison.OrdinalIgnoreCase);

        if (isNamedPipe)
        {
            string pipeName = match.Groups[3].Value;
            if (pipeName.Length > 0)
            {
                var namedInstancePipeMatch = NamedPipeRegex.Match(pipeName);
                if (namedInstancePipeMatch.Success)
                {
                    instanceName = namedInstancePipeMatch.Groups[1].Value;
                    return new SqlConnectionDetails
                    {
                        ServerHostName = serverHostName,
                        ServerIpAddress = serverIpAddress,
                        InstanceName = instanceName,
                        Port = null,
                    };
                }
            }

            return new SqlConnectionDetails
            {
                ServerHostName = serverHostName,
                ServerIpAddress = serverIpAddress,
                InstanceName = null,
                Port = null,
            };
        }

        string port;
        if (match.Groups[4].Length > 0)
        {
            instanceName = match.Groups[3].Value;
            port = match.Groups[4].Value;
            if (port == "1433")
            {
                port = null;
            }
        }
        else if (int.TryParse(match.Groups[3].Value, out int parsedPort))
        {
            port = parsedPort == 1433 ? null : match.Groups[3].Value;
            instanceName = null;
        }
        else
        {
            instanceName = match.Groups[3].Value;

            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = null;
            }

            port = null;
        }

        return new SqlConnectionDetails
        {
            ServerHostName = serverHostName,
            ServerIpAddress = serverIpAddress,
            InstanceName = instanceName,
            Port = port,
        };
    }

    internal void AddConnectionLevelDetailsToActivity(string dataSource, Activity sqlActivity)
    {
        if (!this.EnableConnectionLevelAttributes)
        {
            sqlActivity.SetTag(SemanticConventions.AttributePeerService, dataSource);
        }
        else
        {
            if (!ConnectionDetailCache.TryGetValue(dataSource, out SqlConnectionDetails connectionDetails))
            {
                connectionDetails = ParseDataSource(dataSource);
                ConnectionDetailCache.TryAdd(dataSource, connectionDetails);
            }

            if (!string.IsNullOrEmpty(connectionDetails.InstanceName))
            {
                sqlActivity.SetTag(SemanticConventions.AttributeDbMsSqlInstanceName, connectionDetails.InstanceName);
            }

            if (!string.IsNullOrEmpty(connectionDetails.ServerHostName))
            {
                sqlActivity.SetTag(SemanticConventions.AttributeServerAddress, connectionDetails.ServerHostName);
            }
            else
            {
                sqlActivity.SetTag(SemanticConventions.AttributeServerSocketAddress, connectionDetails.ServerIpAddress);
            }

            if (!string.IsNullOrEmpty(connectionDetails.Port))
            {
                // TODO: Should we continue to emit this if the default port (1433) is being used?
                sqlActivity.SetTag(SemanticConventions.AttributeServerPort, connectionDetails.Port);
            }
        }
    }

    internal sealed class SqlConnectionDetails
    {
        public string ServerHostName { get; set; }

        public string ServerIpAddress { get; set; }

        public string InstanceName { get; set; }

        public string Port { get; set; }
    }
}
