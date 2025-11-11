# Telemetry Storage Implementation Summary

## Overview

This implementation adds a pluggable storage architecture to the Aspire Dashboard for externalizing telemetry data storage. The solution provides a clean separation between the telemetry collection layer and the storage layer, allowing for different storage providers without modifying core telemetry logic.

## Architecture

### Key Components

1. **ITelemetryStorage Interface** (`src/Aspire.Dashboard/Storage/ITelemetryStorage.cs`)
   - Defines the contract for all storage providers
   - Supports logs, traces, and resources
   - Async/await pattern throughout
   - Implements IDisposable for proper cleanup

2. **Storage Provider Types**
   - `InMemoryTelemetryStorage` - Default, fast, volatile storage
   - `SqliteTelemetryStorage` - Persistent storage to SQLite database

3. **TelemetryStorageFactory** (`src/Aspire.Dashboard/Storage/TelemetryStorageFactory.cs`)
   - Factory pattern for creating storage instances
   - Reads configuration from DashboardOptions
   - Validates provider-specific requirements

4. **Configuration Model** (`src/Aspire.Dashboard/Storage/TelemetryStorageOptions.cs`)
   - `ProviderType` - Enum for selecting provider
   - `ConnectionString` - Provider-specific connection string
   - `AutoCreateDatabase` - SQLite-specific auto-creation flag

## Implementation Details

### In-Memory Storage

The `InMemoryTelemetryStorage` class wraps the existing in-memory functionality:
- Uses `List<T>` for storing logs and traces
- Uses `ConcurrentDictionary` for resources
- Enforces configurable size limits (MaxLogCount, MaxTraceCount)
- Thread-safe with ReaderWriterLockSlim

### SQLite Storage

The `SqliteTelemetryStorage` class provides persistent storage:
- Auto-creates database schema on first use
- Stores telemetry data in structured tables:
  - `Resources` - Resource metadata
  - `Logs` - Log entries with full-text storage
  - `Traces` - Trace metadata
  - `Spans` - Individual span data
- Uses JSON serialization for complex objects
- Proper indexing for query performance
- Foreign key constraints for data integrity

### Database Schema

```sql
Resources
├── ResourceKey (PK)
├── Name
├── InstanceId
├── UninstrumentedPeer
├── Data (JSON)
└── CreatedAt

Logs
├── InternalId (PK, AUTOINCREMENT)
├── ResourceKey (FK)
├── TimeStamp (Indexed)
├── TraceId (Indexed)
├── SpanId
├── Severity
├── Message
└── Data (JSON)

Traces
├── TraceId (PK)
├── FirstSpanStartTime
└── Data (JSON)

Spans
├── SpanId (PK)
├── TraceId (FK, Indexed)
├── ResourceKey (FK, Indexed)
├── ParentSpanId
├── Name
├── StartTime (Indexed)
├── EndTime
├── Duration
└── Data (JSON)
```

## Configuration

### JSON Configuration

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

### Environment Variables

```bash
Dashboard__TelemetryStorage__ProviderType=SQLite
Dashboard__TelemetryStorage__ConnectionString="Data Source=/data/telemetry.db"
Dashboard__TelemetryStorage__AutoCreateDatabase=true
```

## Integration with Dashboard

The storage layer is registered in `DashboardWebApplication.cs`:

```csharp
// Register telemetry storage factory and storage
builder.Services.AddSingleton<Storage.TelemetryStorageFactory>();
builder.Services.AddSingleton<Storage.ITelemetryStorage>(sp =>
{
    var factory = sp.GetRequiredService<Storage.TelemetryStorageFactory>();
    return factory.CreateStorage();
});
```

The factory reads from `DashboardOptions.TelemetryStorage` and creates the appropriate storage provider.

## Extensibility

Adding a new storage provider requires:

1. Create a class implementing `ITelemetryStorage`
2. Add a new value to `StorageProviderType` enum
3. Update `TelemetryStorageFactory.CreateStorage()` switch statement
4. Add any provider-specific configuration to `TelemetryStorageOptions`

Example for adding PostgreSQL:

```csharp
public enum StorageProviderType
{
    InMemory,
    SQLite,
    PostgreSQL  // New provider
}

// In TelemetryStorageFactory.CreateStorage():
return storageOptions.ProviderType switch
{
    StorageProviderType.InMemory => CreateInMemoryStorage(),
    StorageProviderType.SQLite => CreateSqliteStorage(storageOptions),
    StorageProviderType.PostgreSQL => CreatePostgreSqlStorage(storageOptions),
    _ => throw new InvalidOperationException(...)
};
```

## Benefits

1. **Separation of Concerns** - Storage logic is separate from telemetry collection
2. **Testability** - Storage providers can be tested independently
3. **Flexibility** - Easy to switch providers or add new ones
4. **Backward Compatibility** - Default in-memory provider maintains existing behavior
5. **Configuration-Driven** - No code changes needed to switch providers

## Current Limitations

1. **TelemetryRepository Integration** - The current implementation coexists with the existing `TelemetryRepository` which still uses `CircularBuffer` directly. Full integration would require refactoring `TelemetryRepository` to use `ITelemetryStorage`.

2. **Metrics Storage** - Current implementation focuses on logs and traces. Metrics storage could be added similarly.

3. **Complex Query Support** - The abstraction supports basic filtering, but complex queries (e.g., full-text search, aggregations) may need provider-specific extensions.

4. **Performance Tuning** - SQLite provider uses JSON serialization which may not be optimal for very high throughput scenarios.

## Future Enhancements

1. **Complete TelemetryRepository Integration** - Refactor to use storage abstraction throughout
2. **Additional Providers** - PostgreSQL, SQL Server, Azure Table Storage, Redis
3. **Batch Operations** - Optimize bulk inserts and updates
4. **Async Initialization** - Make database initialization async
5. **Connection Pooling** - Add pooling for database connections
6. **Query Optimization** - Add provider-specific query optimizations
7. **Data Retention Policies** - Automatic cleanup of old data
8. **Compression** - Compress telemetry data in storage
9. **Partitioning** - Support partitioning for large datasets

## Testing

Tests are located in `tests/Aspire.Dashboard.Tests/Storage/`:
- `TelemetryStorageFactoryTests.cs` - Tests for factory and configuration

Additional integration tests can be added to verify:
- End-to-end storage and retrieval
- Provider-specific features
- Performance characteristics
- Concurrent access patterns

## Documentation

Comprehensive documentation is available in:
- `src/Aspire.Dashboard/Storage/README.md` - User-facing documentation
- `src/Aspire.Dashboard/Storage/appsettings.example.json` - Configuration examples

## Summary

This implementation provides a solid foundation for externalizing Aspire Dashboard telemetry storage. The architecture is extensible, well-documented, and maintains backward compatibility while enabling new scenarios like persistent telemetry data and distributed deployments.
