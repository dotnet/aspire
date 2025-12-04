# Telemetry Storage Providers

The Aspire Dashboard supports pluggable telemetry storage providers to externalize telemetry data storage from in-memory to persistent storage.

## Overview

By default, the Dashboard stores telemetry data (logs, traces, metrics) in memory. This data is lost when the Dashboard restarts. With storage providers, you can persist telemetry data to external storage systems.

## Supported Providers

### In-Memory (Default)

The default storage provider keeps all telemetry data in memory. This is suitable for development and testing scenarios.

**Configuration:**
```json
{
  "Dashboard": {
    "TelemetryStorage": {
      "ProviderType": "InMemory"
    }
  }
}
```

Or using environment variables:
```bash
Dashboard__TelemetryStorage__ProviderType=InMemory
```

### SQLite

The SQLite provider persists telemetry data to a SQLite database file on disk. This is suitable for scenarios where you want to retain telemetry data across Dashboard restarts.

**Configuration:**
```json
{
  "Dashboard": {
    "TelemetryStorage": {
      "ProviderType": "SQLite",
      "ConnectionString": "Data Source=telemetry.db",
      "AutoCreateDatabase": true
    }
  }
}
```

Or using environment variables:
```bash
Dashboard__TelemetryStorage__ProviderType=SQLite
Dashboard__TelemetryStorage__ConnectionString="Data Source=telemetry.db"
Dashboard__TelemetryStorage__AutoCreateDatabase=true
```

**Connection String Format:**

The `ConnectionString` for SQLite follows the standard SQLite connection string format:
- `Data Source=path/to/database.db` - Specifies the database file path
- `Data Source=:memory:` - Uses an in-memory SQLite database (data is lost on restart)

**Options:**
- `AutoCreateDatabase` (default: `true`) - Automatically creates the database and schema if it doesn't exist

## Configuration Options

### TelemetryStorageOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ProviderType` | `StorageProviderType` | `InMemory` | The type of storage provider to use |
| `ConnectionString` | `string?` | `null` | Connection string or file path for the storage provider |
| `AutoCreateDatabase` | `bool` | `true` | Whether to automatically create the database (SQLite only) |

### StorageProviderType Enum

- `InMemory` - In-memory storage (default)
- `SQLite` - SQLite database storage

## Usage Examples

### Development with In-Memory Storage

No configuration needed - this is the default behavior.

### Persistent Storage with SQLite

**appsettings.json:**
```json
{
  "Dashboard": {
    "TelemetryStorage": {
      "ProviderType": "SQLite",
      "ConnectionString": "Data Source=/data/aspire-telemetry.db"
    }
  }
}
```

**Docker environment:**
```bash
docker run -e Dashboard__TelemetryStorage__ProviderType=SQLite \
           -e "Dashboard__TelemetryStorage__ConnectionString=Data Source=/data/telemetry.db" \
           -v /host/data:/data \
           aspire-dashboard
```

## Extending with New Providers

The storage layer is designed to be extensible. To add a new storage provider:

1. Create a new class that implements `ITelemetryStorage`
2. Add a new value to the `StorageProviderType` enum
3. Update `TelemetryStorageFactory.CreateStorage()` to instantiate your provider
4. Register any required dependencies in `DashboardWebApplication`

### ITelemetryStorage Interface

```csharp
public interface ITelemetryStorage : IDisposable
{
    Task AddLogsAsync(IEnumerable<OtlpLogEntry> logs, CancellationToken cancellationToken = default);
    Task<PagedResult<OtlpLogEntry>> GetLogsAsync(GetLogsContext context, CancellationToken cancellationToken = default);
    Task<OtlpLogEntry?> GetLogAsync(long logId, CancellationToken cancellationToken = default);
    
    Task AddTracesAsync(IEnumerable<OtlpTrace> traces, CancellationToken cancellationToken = default);
    Task<GetTracesResponse> GetTracesAsync(GetTracesRequest request, CancellationToken cancellationToken = default);
    Task<OtlpTrace?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);
    
    Task AddOrUpdateResourceAsync(OtlpResource resource, CancellationToken cancellationToken = default);
    Task<List<OtlpResource>> GetResourcesAsync(bool includeUninstrumentedPeers = false, CancellationToken cancellationToken = default);
    Task<OtlpResource?> GetResourceAsync(ResourceKey key, CancellationToken cancellationToken = default);
    
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task<(int LogCount, int TraceCount)> GetCountsAsync(CancellationToken cancellationToken = default);
}
```

## Performance Considerations

### In-Memory Provider

- **Pros**: Fastest performance, no I/O overhead
- **Cons**: Limited by available memory, data lost on restart
- **Best for**: Development, testing, short-lived scenarios

### SQLite Provider

- **Pros**: Persistent storage, good read/write performance, single file database
- **Cons**: Slower than in-memory, concurrent write limitations
- **Best for**: Single-instance deployments, moderate data volumes, local development with persistence needs

## Troubleshooting

### SQLite: "database is locked" errors

SQLite has limitations with concurrent writes. If you experience locking issues:
- Ensure only one Dashboard instance is writing to the database
- Consider using WAL mode by adding `;Mode=ReadWriteCreate;Cache=Shared;Journal Mode=Wal` to your connection string

### SQLite: Permission denied

Ensure the Dashboard process has write permissions to:
- The database file location
- The directory containing the database file (SQLite creates temporary files)

### High memory usage with SQLite

Even with SQLite storage, some data is cached in memory. Monitor the `TelemetryLimits` configuration to control memory usage:

```json
{
  "Dashboard": {
    "TelemetryLimits": {
      "MaxLogCount": 10000,
      "MaxTraceCount": 10000,
      "MaxMetricsCount": 50000
    }
  }
}
```

## Future Storage Providers

The architecture supports adding additional storage providers in the future, such as:
- PostgreSQL
- SQL Server
- Azure Table Storage
- Redis
- Custom implementations

Each provider can be added without modifying existing code, maintaining backward compatibility.
