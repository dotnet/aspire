// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Dashboard.Tests.Storage;

public class TelemetryStorageFactoryTests
{
    [Fact]
    public void CreateStorage_InMemory_ReturnsInMemoryStorage()
    {
        // Arrange
        var options = Options.Create(new DashboardOptions
        {
            TelemetryStorage = new TelemetryStorageOptions
            {
                ProviderType = StorageProviderType.InMemory
            },
            TelemetryLimits = new TelemetryLimitOptions
            {
                MaxLogCount = 1000,
                MaxTraceCount = 1000
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var factory = new TelemetryStorageFactory(
            serviceProvider,
            NullLogger<TelemetryStorageFactory>.Instance,
            options);

        // Act
        var storage = factory.CreateStorage();

        // Assert
        Assert.NotNull(storage);
        Assert.IsType<InMemoryTelemetryStorage>(storage);
        
        storage.Dispose();
    }

    [Fact]
    public void CreateStorage_SQLite_ReturnsSqliteStorage()
    {
        // Arrange
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.db");
        
        try
        {
            var options = Options.Create(new DashboardOptions
            {
                TelemetryStorage = new TelemetryStorageOptions
                {
                    ProviderType = StorageProviderType.SQLite,
                    ConnectionString = $"Data Source={testDbPath}",
                    AutoCreateDatabase = true
                },
                TelemetryLimits = new TelemetryLimitOptions()
            });

            var services = new ServiceCollection();
            services.AddSingleton(options);
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var factory = new TelemetryStorageFactory(
                serviceProvider,
                NullLogger<TelemetryStorageFactory>.Instance,
                options);

            // Act
            var storage = factory.CreateStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.IsType<SqliteTelemetryStorage>(storage);
            
            storage.Dispose();
        }
        finally
        {
            if (File.Exists(testDbPath))
            {
                File.Delete(testDbPath);
            }
        }
    }

    [Fact]
    public void CreateStorage_SQLiteWithoutConnectionString_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new DashboardOptions
        {
            TelemetryStorage = new TelemetryStorageOptions
            {
                ProviderType = StorageProviderType.SQLite,
                ConnectionString = null // Missing connection string
            },
            TelemetryLimits = new TelemetryLimitOptions()
        });

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var factory = new TelemetryStorageFactory(
            serviceProvider,
            NullLogger<TelemetryStorageFactory>.Instance,
            options);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(factory.CreateStorage);
    }
}
