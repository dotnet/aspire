// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Storage;

/// <summary>
/// Factory for creating telemetry storage instances based on configuration.
/// </summary>
internal sealed class TelemetryStorageFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelemetryStorageFactory> _logger;
    private readonly IOptions<DashboardOptions> _dashboardOptions;

    public TelemetryStorageFactory(
        IServiceProvider serviceProvider,
        ILogger<TelemetryStorageFactory> logger,
        IOptions<DashboardOptions> dashboardOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dashboardOptions = dashboardOptions ?? throw new ArgumentNullException(nameof(dashboardOptions));
    }

    /// <summary>
    /// Creates a telemetry storage instance based on the configured provider type.
    /// </summary>
    public ITelemetryStorage CreateStorage()
    {
        var storageOptions = _dashboardOptions.Value.TelemetryStorage;
        
        _logger.LogInformation("Creating telemetry storage with provider type: {ProviderType}", storageOptions.ProviderType);

        return storageOptions.ProviderType switch
        {
            StorageProviderType.InMemory => CreateInMemoryStorage(),
            StorageProviderType.SQLite => CreateSqliteStorage(storageOptions),
            _ => throw new InvalidOperationException($"Unsupported storage provider type: {storageOptions.ProviderType}")
        };
    }

    private ITelemetryStorage CreateInMemoryStorage()
    {
        var telemetryLimits = _dashboardOptions.Value.TelemetryLimits;
        _logger.LogInformation("Using in-memory telemetry storage with limits: MaxLogCount={MaxLogCount}, MaxTraceCount={MaxTraceCount}",
            telemetryLimits.MaxLogCount, telemetryLimits.MaxTraceCount);
        
        return new InMemoryTelemetryStorage(
            telemetryLimits.MaxLogCount,
            telemetryLimits.MaxTraceCount);
    }

    private ITelemetryStorage CreateSqliteStorage(TelemetryStorageOptions storageOptions)
    {
        if (string.IsNullOrWhiteSpace(storageOptions.ConnectionString))
        {
            throw new InvalidOperationException("SQLite storage requires a ConnectionString to be configured.");
        }

        _logger.LogInformation("Using SQLite telemetry storage at: {ConnectionString}", storageOptions.ConnectionString);

        var sqliteLogger = _serviceProvider.GetRequiredService<ILogger<SqliteTelemetryStorage>>();
        
        return new SqliteTelemetryStorage(
            storageOptions.ConnectionString,
            storageOptions.AutoCreateDatabase,
            sqliteLogger);
    }
}
