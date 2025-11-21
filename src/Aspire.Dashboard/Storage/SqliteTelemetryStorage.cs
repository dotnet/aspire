// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.Data.Sqlite;

namespace Aspire.Dashboard.Storage;

/// <summary>
/// SQLite implementation of ITelemetryStorage.
/// Provides persistent storage for telemetry data.
/// </summary>
internal sealed class SqliteTelemetryStorage : ITelemetryStorage
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteTelemetryStorage> _logger;
    private readonly bool _autoCreateDatabase;

    public SqliteTelemetryStorage(
        string connectionString,
        bool autoCreateDatabase,
        ILogger<SqliteTelemetryStorage> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _autoCreateDatabase = autoCreateDatabase;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_autoCreateDatabase)
        {
            InitializeDatabase();
        }
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Resources (
                ResourceKey TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                InstanceId TEXT NOT NULL,
                UninstrumentedPeer INTEGER NOT NULL,
                Data TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Logs (
                InternalId INTEGER PRIMARY KEY AUTOINCREMENT,
                ResourceKey TEXT NOT NULL,
                TimeStamp TEXT NOT NULL,
                TraceId TEXT,
                SpanId TEXT,
                Severity INTEGER NOT NULL,
                Message TEXT,
                Data TEXT NOT NULL,
                FOREIGN KEY(ResourceKey) REFERENCES Resources(ResourceKey)
            );

            CREATE INDEX IF NOT EXISTS idx_logs_resource ON Logs(ResourceKey);
            CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON Logs(TimeStamp);
            CREATE INDEX IF NOT EXISTS idx_logs_traceid ON Logs(TraceId);

            CREATE TABLE IF NOT EXISTS Traces (
                TraceId TEXT PRIMARY KEY,
                FirstSpanStartTime TEXT NOT NULL,
                Data TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Spans (
                SpanId TEXT PRIMARY KEY,
                TraceId TEXT NOT NULL,
                ResourceKey TEXT NOT NULL,
                ParentSpanId TEXT,
                Name TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT,
                Duration INTEGER,
                Data TEXT NOT NULL,
                FOREIGN KEY(TraceId) REFERENCES Traces(TraceId),
                FOREIGN KEY(ResourceKey) REFERENCES Resources(ResourceKey)
            );

            CREATE INDEX IF NOT EXISTS idx_spans_traceid ON Spans(TraceId);
            CREATE INDEX IF NOT EXISTS idx_spans_resource ON Spans(ResourceKey);
            CREATE INDEX IF NOT EXISTS idx_spans_starttime ON Spans(StartTime);
        ";

        command.ExecuteNonQuery();
        _logger.LogInformation("SQLite database initialized at {ConnectionString}", _connectionString);
    }

    public async Task AddLogsAsync(IEnumerable<OtlpLogEntry> logs, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var transaction = connection.BeginTransaction();
        
        try
        {
            foreach (var log in logs)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO Logs (ResourceKey, TimeStamp, TraceId, SpanId, Severity, Message, Data)
                    VALUES (@ResourceKey, @TimeStamp, @TraceId, @SpanId, @Severity, @Message, @Data)
                ";

                command.Parameters.AddWithValue("@ResourceKey", log.ResourceView.ResourceKey.ToString());
                command.Parameters.AddWithValue("@TimeStamp", log.TimeStamp.ToString("o"));
                command.Parameters.AddWithValue("@TraceId", log.TraceId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SpanId", log.SpanId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Severity", (int)log.Severity);
                command.Parameters.AddWithValue("@Message", log.Message ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Data", JsonSerializer.Serialize(log));

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding logs to SQLite database");
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<PagedResult<OtlpLogEntry>> GetLogsAsync(GetLogsContext context, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Build query with filters
        var queryBuilder = new System.Text.StringBuilder("SELECT Data FROM Logs WHERE 1=1");
        var parameters = new List<SqliteParameter>();

        if (context.ResourceKey.HasValue)
        {
            queryBuilder.Append(" AND ResourceKey = @ResourceKey");
            parameters.Add(new SqliteParameter("@ResourceKey", context.ResourceKey.Value.ToString()));
        }

        // Count total matching records
        var countQuery = queryBuilder.ToString().Replace("SELECT Data FROM Logs", "SELECT COUNT(*) FROM Logs");
        int totalCount;
        
        using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = countQuery;
            countCommand.Parameters.AddRange(parameters.ToArray());
            totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);
        }

        // Get paged results
        queryBuilder.Append(" ORDER BY TimeStamp DESC LIMIT @Limit OFFSET @Offset");
        parameters.Add(new SqliteParameter("@Limit", context.Count));
        parameters.Add(new SqliteParameter("@Offset", context.StartIndex));

        var logs = new List<OtlpLogEntry>();
        
        using (var command = connection.CreateCommand())
        {
            command.CommandText = queryBuilder.ToString();
            command.Parameters.AddRange(parameters.ToArray());

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var data = reader.GetString(0);
                var log = JsonSerializer.Deserialize<OtlpLogEntry>(data);
                if (log != null)
                {
                    logs.Add(log);
                }
            }
        }

        return new PagedResult<OtlpLogEntry>
        {
            Items = logs,
            TotalItemCount = totalCount,
            IsFull = false // SQLite doesn't have a hard limit
        };
    }

    public async Task<OtlpLogEntry?> GetLogAsync(long logId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM Logs WHERE InternalId = @LogId";
        command.Parameters.AddWithValue("@LogId", logId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var data = reader.GetString(0);
            return JsonSerializer.Deserialize<OtlpLogEntry>(data);
        }

        return null;
    }

    public async Task AddTracesAsync(IEnumerable<OtlpTrace> traces, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var transaction = connection.BeginTransaction();
        
        try
        {
            foreach (var trace in traces)
            {
                // Insert trace
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT OR REPLACE INTO Traces (TraceId, FirstSpanStartTime, Data)
                        VALUES (@TraceId, @FirstSpanStartTime, @Data)
                    ";

                    command.Parameters.AddWithValue("@TraceId", trace.TraceId);
                    command.Parameters.AddWithValue("@FirstSpanStartTime", trace.FirstSpan.StartTime.ToString("o"));
                    command.Parameters.AddWithValue("@Data", JsonSerializer.Serialize(trace));

                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                // Insert spans
                foreach (var span in trace.Spans)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT OR REPLACE INTO Spans (SpanId, TraceId, ResourceKey, ParentSpanId, Name, StartTime, EndTime, Duration, Data)
                        VALUES (@SpanId, @TraceId, @ResourceKey, @ParentSpanId, @Name, @StartTime, @EndTime, @Duration, @Data)
                    ";

                    command.Parameters.AddWithValue("@SpanId", span.SpanId);
                    command.Parameters.AddWithValue("@TraceId", trace.TraceId);
                    command.Parameters.AddWithValue("@ResourceKey", span.Source.ResourceKey.ToString());
                    command.Parameters.AddWithValue("@ParentSpanId", span.ParentSpanId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Name", span.Name);
                    command.Parameters.AddWithValue("@StartTime", span.StartTime.ToString("o"));
                    command.Parameters.AddWithValue("@EndTime", span.EndTime.ToString("o"));
                    command.Parameters.AddWithValue("@Duration", span.Duration.Ticks);
                    command.Parameters.AddWithValue("@Data", JsonSerializer.Serialize(span));

                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding traces to SQLite database");
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<GetTracesResponse> GetTracesAsync(GetTracesRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var queryBuilder = new System.Text.StringBuilder("SELECT Data FROM Traces WHERE 1=1");
        var parameters = new List<SqliteParameter>();

        // Count total matching records
        var countQuery = queryBuilder.ToString().Replace("SELECT Data FROM Traces", "SELECT COUNT(*) FROM Traces");
        int totalCount;
        
        using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = countQuery;
            totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);
        }

        // Get paged results
        queryBuilder.Append(" ORDER BY FirstSpanStartTime DESC LIMIT @Limit OFFSET @Offset");
        parameters.Add(new SqliteParameter("@Limit", request.Count));
        parameters.Add(new SqliteParameter("@Offset", request.StartIndex));

        var traces = new List<OtlpTrace>();
        
        using (var command = connection.CreateCommand())
        {
            command.CommandText = queryBuilder.ToString();
            command.Parameters.AddRange(parameters.ToArray());

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var data = reader.GetString(0);
                var trace = JsonSerializer.Deserialize<OtlpTrace>(data);
                if (trace != null)
                {
                    traces.Add(trace);
                }
            }
        }

        var maxDuration = traces.Count > 0 
            ? traces.Max(t => t.Duration)
            : TimeSpan.Zero;

        return new GetTracesResponse
        {
            PagedResult = new PagedResult<OtlpTrace>
            {
                Items = traces,
                TotalItemCount = totalCount,
                IsFull = false
            },
            MaxDuration = maxDuration
        };
    }

    public async Task<OtlpTrace?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM Traces WHERE TraceId = @TraceId";
        command.Parameters.AddWithValue("@TraceId", traceId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var data = reader.GetString(0);
            return JsonSerializer.Deserialize<OtlpTrace>(data);
        }

        return null;
    }

    public async Task AddOrUpdateResourceAsync(OtlpResource resource, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Resources (ResourceKey, Name, InstanceId, UninstrumentedPeer, Data, CreatedAt)
            VALUES (@ResourceKey, @Name, @InstanceId, @UninstrumentedPeer, @Data, @CreatedAt)
        ";

        command.Parameters.AddWithValue("@ResourceKey", resource.ResourceKey.ToString());
        command.Parameters.AddWithValue("@Name", resource.ResourceKey.Name);
        command.Parameters.AddWithValue("@InstanceId", resource.ResourceKey.InstanceId ?? string.Empty);
        command.Parameters.AddWithValue("@UninstrumentedPeer", resource.UninstrumentedPeer ? 1 : 0);
        command.Parameters.AddWithValue("@Data", JsonSerializer.Serialize(resource));
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<OtlpResource>> GetResourcesAsync(bool includeUninstrumentedPeers = false, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var query = "SELECT Data FROM Resources";
        if (!includeUninstrumentedPeers)
        {
            query += " WHERE UninstrumentedPeer = 0";
        }
        query += " ORDER BY Name, InstanceId";

        using var command = connection.CreateCommand();
        command.CommandText = query;

        var resources = new List<OtlpResource>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var data = reader.GetString(0);
            var resource = JsonSerializer.Deserialize<OtlpResource>(data);
            if (resource != null)
            {
                resources.Add(resource);
            }
        }

        return resources;
    }

    public async Task<OtlpResource?> GetResourceAsync(ResourceKey key, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM Resources WHERE ResourceKey = @ResourceKey";
        command.Parameters.AddWithValue("@ResourceKey", key.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var data = reader.GetString(0);
            return JsonSerializer.Deserialize<OtlpResource>(data);
        }

        return null;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var transaction = connection.BeginTransaction();
        
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                DELETE FROM Spans;
                DELETE FROM Traces;
                DELETE FROM Logs;
                DELETE FROM Resources;
            ";

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation("Cleared all data from SQLite database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing SQLite database");
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<(int LogCount, int TraceCount)> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                (SELECT COUNT(*) FROM Logs) as LogCount,
                (SELECT COUNT(*) FROM Traces) as TraceCount
        ";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (reader.GetInt32(0), reader.GetInt32(1));
        }

        return (0, 0);
    }

    public void Dispose()
    {
        // SQLite connections are disposed when they go out of scope
    }
}
