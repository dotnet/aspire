// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable disable

#if !NETFRAMEWORK
using System.Data;
using System.Diagnostics;
using OpenTelemetry.Trace;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace OpenTelemetry.Instrumentation.SqlClient.Implementation;

#if NET6_0_OR_GREATER
[RequiresUnreferencedCode(SqlClientInstrumentation.SqlClientTrimmingUnsupportedMessage)]
#endif
internal sealed class SqlClientDiagnosticListener : ListenerHandler
{
    public const string SqlDataBeforeExecuteCommand = "System.Data.SqlClient.WriteCommandBefore";
    public const string SqlMicrosoftBeforeExecuteCommand = "Microsoft.Data.SqlClient.WriteCommandBefore";

    public const string SqlDataAfterExecuteCommand = "System.Data.SqlClient.WriteCommandAfter";
    public const string SqlMicrosoftAfterExecuteCommand = "Microsoft.Data.SqlClient.WriteCommandAfter";

    public const string SqlDataWriteCommandError = "System.Data.SqlClient.WriteCommandError";
    public const string SqlMicrosoftWriteCommandError = "Microsoft.Data.SqlClient.WriteCommandError";

    private readonly PropertyFetcher<object> commandFetcher = new("Command");
    private readonly PropertyFetcher<object> connectionFetcher = new("Connection");
    private readonly PropertyFetcher<object> dataSourceFetcher = new("DataSource");
    private readonly PropertyFetcher<object> databaseFetcher = new("Database");
    private readonly PropertyFetcher<CommandType> commandTypeFetcher = new("CommandType");
    private readonly PropertyFetcher<object> commandTextFetcher = new("CommandText");
    private readonly PropertyFetcher<Exception> exceptionFetcher = new("Exception");
    private readonly SqlClientTraceInstrumentationOptions options;

    public SqlClientDiagnosticListener(string sourceName, SqlClientTraceInstrumentationOptions options)
        : base(sourceName)
    {
        this.options = options ?? new SqlClientTraceInstrumentationOptions();
    }

    public override bool SupportsNullActivity => true;

    public override void OnEventWritten(string name, object payload)
    {
        var activity = Activity.Current;
        switch (name)
        {
            case SqlDataBeforeExecuteCommand:
            case SqlMicrosoftBeforeExecuteCommand:
                {
                    // SqlClient does not create an Activity. So the activity coming in here will be null or the root span.
                    activity = SqlActivitySourceHelper.ActivitySource.StartActivity(
                        SqlActivitySourceHelper.ActivityName,
                        ActivityKind.Client,
                        default(ActivityContext),
                        SqlActivitySourceHelper.CreationTags);

                    if (activity == null)
                    {
                        // There is no listener or it decided not to sample the current request.
                        return;
                    }

                    _ = this.commandFetcher.TryFetch(payload, out var command);
                    if (command == null)
                    {
                        SqlClientInstrumentationEventSource.Log.NullPayload(nameof(SqlClientDiagnosticListener), name);
                        activity.Stop();
                        return;
                    }

                    if (activity.IsAllDataRequested)
                    {
                        try
                        {
                            if (this.options.Filter?.Invoke(command) == false)
                            {
                                SqlClientInstrumentationEventSource.Log.CommandIsFilteredOut(activity.OperationName);
                                activity.IsAllDataRequested = false;
                                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            SqlClientInstrumentationEventSource.Log.CommandFilterException(ex);
                            activity.IsAllDataRequested = false;
                            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                            return;
                        }

                        _ = this.connectionFetcher.TryFetch(command, out var connection);
                        _ = this.databaseFetcher.TryFetch(connection, out var database);

                        activity.DisplayName = (string)database;

                        _ = this.dataSourceFetcher.TryFetch(connection, out var dataSource);
                        _ = this.commandTextFetcher.TryFetch(command, out var commandText);

                        activity.SetTag(SemanticConventions.AttributeDbName, (string)database);

                        this.options.AddConnectionLevelDetailsToActivity((string)dataSource, activity);

                        if (this.commandTypeFetcher.TryFetch(command, out CommandType commandType))
                        {
                            switch (commandType)
                            {
                                case CommandType.StoredProcedure:
                                    if (this.options.SetDbStatementForStoredProcedure)
                                    {
                                        activity.SetTag(SemanticConventions.AttributeDbStatement, (string)commandText);
                                    }

                                    break;

                                case CommandType.Text:
                                    if (this.options.SetDbStatementForText)
                                    {
                                        activity.SetTag(SemanticConventions.AttributeDbStatement, (string)commandText);
                                    }

                                    break;

                                case CommandType.TableDirect:
                                    break;
                            }
                        }

                        try
                        {
                            this.options.Enrich?.Invoke(activity, "OnCustom", command);
                        }
                        catch (Exception ex)
                        {
                            SqlClientInstrumentationEventSource.Log.EnrichmentException(ex);
                        }
                    }
                }

                break;
            case SqlDataAfterExecuteCommand:
            case SqlMicrosoftAfterExecuteCommand:
                {
                    if (activity == null)
                    {
                        SqlClientInstrumentationEventSource.Log.NullActivity(name);
                        return;
                    }

                    if (activity.Source != SqlActivitySourceHelper.ActivitySource)
                    {
                        return;
                    }

                    activity.Stop();
                }

                break;
            case SqlDataWriteCommandError:
            case SqlMicrosoftWriteCommandError:
                {
                    if (activity == null)
                    {
                        SqlClientInstrumentationEventSource.Log.NullActivity(name);
                        return;
                    }

                    if (activity.Source != SqlActivitySourceHelper.ActivitySource)
                    {
                        return;
                    }

                    try
                    {
                        if (activity.IsAllDataRequested)
                        {
                            if (this.exceptionFetcher.TryFetch(payload, out Exception exception) && exception != null)
                            {
                                activity.SetStatus(ActivityStatusCode.Error, exception.Message);

                                if (this.options.RecordException)
                                {
                                    activity.AddException(exception);
                                }
                            }
                            else
                            {
                                SqlClientInstrumentationEventSource.Log.NullPayload(nameof(SqlClientDiagnosticListener), name);
                            }
                        }
                    }
                    finally
                    {
                        activity.Stop();
                    }
                }

                break;
        }
    }
}
#endif
