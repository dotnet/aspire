// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class DashboardResourceTests
{
    [Fact]
    public async Task DashboardIsAutomaticallyAddedAsHiddenResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Ensure any ambient configuration doesn't impact this test.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DOTNET_ASPIRE_SHOW_DASHBOARD_RESOURCES"] = null
        });

        var dashboardPath = Path.GetFullPath("dashboard");

        builder.Services.Configure<DcpOptions>(o =>
        {
            o.DashboardPath = dashboardPath;
        });

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dashboard = Assert.Single(model.Resources.OfType<ExecutableResource>());
        var initialSnapshot = Assert.Single(dashboard.Annotations.OfType<ResourceSnapshotAnnotation>());

        Assert.NotNull(dashboard);
        Assert.Equal("aspire-dashboard", dashboard.Name);
        Assert.Equal(dashboardPath, dashboard.Command);
        Assert.Equal("Hidden", initialSnapshot.InitialSnapshot.State);
    }

    [Fact]
    public async Task DashboardIsAddedFirst()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddContainer("my-container", "my-image");

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Collection(model.Resources,
            r => Assert.Equal("aspire-dashboard", r.Name),
            r => Assert.Equal("my-container", r.Name)
        );
    }

    [Fact]
    public async Task DashboardDoesNotAddResource_ConfiguresExistingDashboard()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Services.AddSingleton<IDashboardEndpointProvider, MockDashboardEndpointProvider>();

        builder.Configuration.Sources.Clear();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_URLS"] = "http://localhost",
            ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost"
        });

        var container = builder.AddContainer(KnownResourceNames.AspireDashboard, "my-image");

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dashboard = Assert.Single(model.Resources);

        Assert.Same(container.Resource, dashboard);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(dashboard);

        Assert.Collection(config,
            e =>
            {
                Assert.Equal("ASPNETCORE_ENVIRONMENT", e.Key);
                Assert.Equal("Production", e.Value);
            },
            e =>
            {
                Assert.Equal("ASPNETCORE_URLS", e.Key);
                Assert.Equal("http://localhost", e.Value);
            },
            e =>
            {
                Assert.Equal("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL", e.Key);
                Assert.Equal("http://localhost:5000", e.Value);
            },
            e =>
            {
                Assert.Equal("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", e.Key);
                Assert.Equal("http://localhost", e.Value);
            },
            e =>
            {
                Assert.Equal("DASHBOARD__FRONTEND__AUTHMODE", e.Key);
                Assert.Equal("Unsecured", e.Value);
            },
            e =>
            {
                Assert.Equal("DASHBOARD__RESOURCESERVICECLIENT__AUTHMODE", e.Key);
                Assert.Equal("Unsecured", e.Value);
            },
            e =>
            {
                Assert.Equal("DASHBOARD__OTLP__AUTHMODE", e.Key);
                Assert.Equal("Unsecured", e.Value);
            },
            e =>
            {
                Assert.Equal("LOGGING__CONSOLE__FORMATTERNAME", e.Key);
                Assert.Equal("json", e.Value);
            }
        );
    }

    [Fact]
    public async Task DashboardWithDllPathLaunchesDotnet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var dashboardPath = Path.GetFullPath("dashboard.dll");

        builder.Services.Configure<DcpOptions>(o =>
        {
            o.DashboardPath = dashboardPath;
        });

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dashboard = Assert.Single(model.Resources.OfType<ExecutableResource>());

        var args = await ArgumentEvaluator.GetArgumentListAsync(dashboard);

        Assert.NotNull(dashboard);
        Assert.Equal("aspire-dashboard", dashboard.Name);
        Assert.Equal("dotnet", dashboard.Command);
        Assert.Equal([dashboardPath], args);
    }

    [Fact]
    public async Task DashboardAuthConfigured_EnvVarsPresent()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Services.AddSingleton<IDashboardEndpointProvider, MockDashboardEndpointProvider>();

        builder.Configuration.Sources.Clear();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_URLS"] = "http://localhost",
            ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost",
            ["AppHost:BrowserToken"] = "TestBrowserToken!",
            ["AppHost:OtlpApiKey"] = "TestOtlpApiKey!"
        });

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dashboard = Assert.Single(model.Resources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(dashboard);

        Assert.Equal("BrowserToken", config.Single(e => e.Key == DashboardConfigNames.DashboardFrontendAuthModeName.EnvVarName).Value);
        Assert.Equal("TestBrowserToken!", config.Single(e => e.Key == DashboardConfigNames.DashboardFrontendBrowserTokenName.EnvVarName).Value);

        Assert.Equal("ApiKey", config.Single(e => e.Key == DashboardConfigNames.DashboardOtlpAuthModeName.EnvVarName).Value);
        Assert.Equal("TestOtlpApiKey!", config.Single(e => e.Key == DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.EnvVarName).Value);
    }

    [Fact]
    public async Task DashboardAuthRemoved_EnvVarsUnsecured()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Services.AddSingleton<IDashboardEndpointProvider, MockDashboardEndpointProvider>();

        builder.Configuration.Sources.Clear();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_URLS"] = "http://localhost",
            ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost"
        });

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dashboard = Assert.Single(model.Resources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(dashboard);

        Assert.Equal("Unsecured", config.Single(e => e.Key == DashboardConfigNames.DashboardFrontendAuthModeName.EnvVarName).Value);
        Assert.Equal("Unsecured", config.Single(e => e.Key == DashboardConfigNames.DashboardOtlpAuthModeName.EnvVarName).Value);
    }

    [Fact]
    public async Task DashboardResourceServiceUriIsSet()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Services.AddSingleton<IDashboardEndpointProvider, MockDashboardEndpointProvider>();

        builder.Configuration.Sources.Clear();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_URLS"] = "http://localhost",
            ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost"
        });

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dashboard = Assert.Single(model.Resources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(dashboard);

        Assert.Equal("http://localhost:5000", config.Single(e => e.Key == DashboardConfigNames.ResourceServiceUrlName.EnvVarName).Value);
    }

    [Fact]
    public async Task DashboardIsNotAddedInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Empty(model.Resources);
    }

    [Fact]
    public async Task DashboardIsNotAddedIfDisabled()
    {
        using var builder = TestDistributedApplicationBuilder.Create(new DistributedApplicationOptions { DisableDashboard = true });

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        Assert.Empty(model.Resources);
    }

    [Fact]
    public void ContainerIsValidWithDashboardIsDisabled()
    {
        // Set the host environment to "Development" so that the container validates services.
        using var builder = TestDistributedApplicationBuilder.Create(new DistributedApplicationOptions
        {
            DisableDashboard = true,
            Args = ["--environment", "Development"]
        }
        );

        // Container validation logic runs when the service provider is built.
        using var app = builder.Build();
    }

    [Theory]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public async Task DashboardLifecycleHookWatchesLogs(LogLevel logLevel)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var loggerProvider = new TestLoggerProvider();

        builder.Services.AddLogging(b =>
        {
            b.AddProvider(loggerProvider);
            b.AddFilter("Aspire.Hosting.Dashboard", logLevel);
        });

        var dashboardPath = Path.GetFullPath("dashboard");

        builder.Services.Configure<DcpOptions>(o =>
        {
            o.DashboardPath = dashboardPath;
        });

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resourceNotifcationService = app.Services.GetRequiredService<ResourceNotificationService>();
        var resourceLoggerService = app.Services.GetRequiredService<ResourceLoggerService>();

        var dashboard = Assert.Single(model.Resources.OfType<ExecutableResource>());

        Assert.NotNull(dashboard);
        Assert.Equal("aspire-dashboard", dashboard.Name);

        // Push a notification through to the dashboard resource.
        await resourceNotifcationService.PublishUpdateAsync(dashboard, "aspire-dashboard-0", s => s with { State = "Running" });

        // Push some logs through to the dashboard resource.
        var logger = resourceLoggerService.GetLogger("aspire-dashboard-0");

        // The logging watcher expects a JSON payload
        var dashboardLogMessage = new DashboardLogMessage
        {
            Category = "Test",
            LogLevel = logLevel,
            Message = "Test dashboard message"
        };

        logger.Log(logLevel, 0, JsonSerializer.Serialize(dashboardLogMessage), null, (s, _) => s);

        // Get the logger with the category we expect Aspire.Hosting.Dashboard.Test
        var testLogger = loggerProvider.CreateLogger("Aspire.Hosting.Dashboard.Test") as TestLogger;

        Assert.NotNull(testLogger);

        // Get the first log message that was logged
        var log = await testLogger.FirstLogTask.WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal("Test dashboard message", log.Message);
        Assert.Equal(logLevel, log.LogLevel);

    }

    [Fact]
    public async Task DashboardIsExcludedFromManifestInPublishModeEvenIfAddedExplicitly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddProject<DashboardProject>(KnownResourceNames.AspireDashboard);

        var app = builder.Build();

        await app.ExecuteBeforeStartHooksAsync(default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dashboard = Assert.Single(model.Resources.OfType<ProjectResource>());

        Assert.NotNull(dashboard);
        var annotation = Assert.Single(dashboard.Annotations.OfType<ManifestPublishingCallbackAnnotation>());

        var manifest = await ManifestUtils.GetManifestOrNull(dashboard);

        Assert.Equal("aspire-dashboard", dashboard.Name);
        Assert.Same(ManifestPublishingCallbackAnnotation.Ignore, annotation);
        Assert.Null(manifest);
    }

    private sealed class DashboardProject : IProjectMetadata
    {
        public string ProjectPath => "dashboard.csproj";

        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class TestLogger : ILogger
    {
        private readonly TaskCompletionSource<LogMessage> _tcs = new();

        public Task<LogMessage> FirstLogTask => _tcs.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = new LogMessage
            {
                LogLevel = logLevel,
                Message = formatter(state, exception)
            };

            _tcs.TrySetResult(message);
        }
            

        public sealed class LogMessage
        {
            public LogLevel LogLevel { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }

    private sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, TestLogger> _loggers = new();

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, _ => new TestLogger());
        }

        public void Dispose() { }
    }

    private sealed class MockDashboardEndpointProvider : IDashboardEndpointProvider
    {
        public Task<string> GetResourceServiceUriAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("http://localhost:5000");
        }
    }
}
