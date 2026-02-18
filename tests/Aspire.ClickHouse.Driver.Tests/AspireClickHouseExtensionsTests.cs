// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.ClickHouse.Driver.Tests;

public class AspireClickHouseExtensionsTests : IClassFixture<ClickHouseContainerFixture>
{
    private const string TestConnectionString = "Host=localhost;Port=8123;Username=default;Password=;Database=default";

    private readonly ClickHouseContainerFixture _containerFixture;

    public AspireClickHouseExtensionsTests(ClickHouseContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:clickhouse", TestConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedClickHouseDataSource("clickhouse");
        }
        else
        {
            builder.AddClickHouseDataSource("clickhouse");
        }

        using var host = builder.Build();
        var dataSource = useKeyed
            ? host.Services.GetRequiredKeyedService<ClickHouseDataSource>("clickhouse")
            : host.Services.GetRequiredService<ClickHouseDataSource>();

        Assert.NotNull(dataSource);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:clickhouse", "Host=config-host;Port=8123")
        ]);

        static void SetConnectionString(ClickHouseClientSettings settings) => settings.ConnectionString = TestConnectionString;
        if (useKeyed)
        {
            builder.AddKeyedClickHouseDataSource("clickhouse", SetConnectionString);
        }
        else
        {
            builder.AddClickHouseDataSource("clickhouse", SetConnectionString);
        }

        using var host = builder.Build();
        var dataSource = useKeyed
            ? host.Services.GetRequiredKeyedService<ClickHouseDataSource>("clickhouse")
            : host.Services.GetRequiredService<ClickHouseDataSource>();

        Assert.NotNull(dataSource);
        Assert.Contains("localhost", dataSource.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "clickhouse" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:ClickHouse:Driver", key, "ConnectionString"), "Host=unused;Port=8123"),
            new KeyValuePair<string, string?>("ConnectionStrings:clickhouse", TestConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedClickHouseDataSource("clickhouse");
        }
        else
        {
            builder.AddClickHouseDataSource("clickhouse");
        }

        using var host = builder.Build();
        var dataSource = useKeyed
            ? host.Services.GetRequiredKeyedService<ClickHouseDataSource>("clickhouse")
            : host.Services.GetRequiredService<ClickHouseDataSource>();

        Assert.Contains("localhost", dataSource.ConnectionString);
        Assert.DoesNotContain("unused", dataSource.ConnectionString);
    }

    [Fact]
    public void HealthCheckRegisteredByDefault()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:clickhouse", TestConnectionString)
        ]);

        builder.AddClickHouseDataSource("clickhouse");

        Assert.True(((IHostApplicationBuilder)builder).Properties.ContainsKey("Aspire.HealthChecks.ClickHouse"));
    }

    [Fact]
    public void HealthCheckSkippedWhenDisabled()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:clickhouse", TestConnectionString),
            new KeyValuePair<string, string?>("Aspire:ClickHouse:Driver:DisableHealthChecks", "true")
        ]);

        builder.AddClickHouseDataSource("clickhouse");

        Assert.False(((IHostApplicationBuilder)builder).Properties.ContainsKey("Aspire.HealthChecks.ClickHouse"));
    }

    [Fact]
    public void KeyedServiceHealthCheckNameIncludesConnectionName()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:analytics", TestConnectionString)
        ]);

        builder.AddKeyedClickHouseDataSource("analytics");

        Assert.True(((IHostApplicationBuilder)builder).Properties.ContainsKey("Aspire.HealthChecks.ClickHouse_analytics"));
    }

    [Fact]
    public void MissingConnectionStringThrowsOnResolve()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddClickHouseDataSource("clickhouse");

        using var host = builder.Build();
        var ex = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<ClickHouseDataSource>);
        Assert.Contains("ConnectionString is missing", ex.Message);
        Assert.Contains("ConnectionStrings:clickhouse", ex.Message);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:ch1", "Host=localhost1;Port=8123"),
            new KeyValuePair<string, string?>("ConnectionStrings:ch2", "Host=localhost2;Port=8123"),
            new KeyValuePair<string, string?>("ConnectionStrings:ch3", "Host=localhost3;Port=8123"),
        ]);

        builder.AddClickHouseDataSource("ch1");
        builder.AddKeyedClickHouseDataSource("ch2");
        builder.AddKeyedClickHouseDataSource("ch3");

        using var host = builder.Build();

        var ds1 = host.Services.GetRequiredService<ClickHouseDataSource>();
        var ds2 = host.Services.GetRequiredKeyedService<ClickHouseDataSource>("ch2");
        var ds3 = host.Services.GetRequiredKeyedService<ClickHouseDataSource>("ch3");

        Assert.NotSame(ds1, ds2);
        Assert.NotSame(ds1, ds3);
        Assert.NotSame(ds2, ds3);

        Assert.Contains("localhost1", ds1.ConnectionString);
        Assert.Contains("localhost2", ds2.ConnectionString);
        Assert.Contains("localhost3", ds3.ConnectionString);
    }

    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    public async Task HealthCheckReportsHealthyWithRealServer()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:clickhouse", _containerFixture.GetConnectionString())
        ]);

        builder.AddClickHouseDataSource("clickhouse");

        using var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync();

        Assert.Contains(report.Entries, x => x.Key == "ClickHouse");
        Assert.Equal(HealthStatus.Healthy, report.Entries["ClickHouse"].Status);
    }
}
