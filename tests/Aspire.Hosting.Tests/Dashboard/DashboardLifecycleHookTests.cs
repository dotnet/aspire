// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Hosting.ConsoleLogs;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Devcontainers;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class DashboardLifecycleHookTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [MemberData(nameof(Data))]
    public async Task WatchDashboardLogs_WrittenToHostLoggerFactory(DateTime? timestamp, string logMessage, string expectedMessage, string expectedCategory, LogLevel expectedLevel)
    {
        // Arrange
        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddProvider(new TestLoggerProvider(testSink));
            b.AddXunit(testOutputHelper);
        });
        var logChannel = Channel.CreateUnbounded<WriteContext>();
        testSink.MessageLogged += c => logChannel.Writer.TryWrite(c);

        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create(logger: factory.CreateLogger<ResourceNotificationService>());
        var configuration = new ConfigurationBuilder().Build();
        var hook = CreateHook(resourceLoggerService, resourceNotificationService, configuration, loggerFactory: factory);

        var model = new DistributedApplicationModel(new ResourceCollection());
        await hook.BeforeStartAsync(model, CancellationToken.None).DefaultTimeout();

        await resourceNotificationService.PublishUpdateAsync(model.Resources.Single(), s => s).DefaultTimeout();

        string resourceId = default!;
        await foreach (var item in resourceLoggerService.WatchAnySubscribersAsync().DefaultTimeout())
        {
            if (item.Name.StartsWith(KnownResourceNames.AspireDashboard) && item.AnySubscribers)
            {
                resourceId = item.Name;
                break;
            }
        }

        // Act
        var dashboardLoggerState = resourceLoggerService.GetResourceLoggerState(resourceId);
        dashboardLoggerState.AddLog(LogEntry.Create(timestamp, logMessage, isErrorMessage: false), inMemorySource: true);

        // Assert
        while (true)
        {
            var logContext = await logChannel.Reader.ReadAsync().DefaultTimeout();
            if (logContext.LoggerName == expectedCategory)
            {
                Assert.Equal(expectedMessage, logContext.Message);
                Assert.Equal(expectedLevel, logContext.LogLevel);
                break;
            }
        }
    }

    [Fact]
    public async Task BeforeStartAsync_ExcludeLifecycleCommands_CommandsNotAddedToDashboard()
    {
        // Arrange
        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();
        var configuration = new ConfigurationBuilder().Build();
        var hook = CreateHook(resourceLoggerService, resourceNotificationService, configuration);

        var model = new DistributedApplicationModel(new ResourceCollection());

        // Act
        await hook.BeforeStartAsync(model, CancellationToken.None).DefaultTimeout();
        var dashboardResource = model.Resources.Single(r => string.Equals(r.Name, KnownResourceNames.AspireDashboard, StringComparisons.ResourceName));
        dashboardResource.AddLifeCycleCommands();

        // Assert
        Assert.Single(dashboardResource.Annotations.OfType<ExcludeLifecycleCommandsAnnotation>());
        Assert.Empty(dashboardResource.Annotations.OfType<ResourceCommandAnnotation>());
    }

    [Theory]
    [InlineData("localhost:8080", 8080, "1234", "cert", true)]
    [InlineData("localhost:8080", 8080, "1234", "cert", false)]
    [InlineData(null, null, null, null, null)]
    public async Task BeforeStartAsync_DashboardContainsDebugSessionInfo(string? debugSessionPort, int? expectedDebugSessionPort, string? debugSessionToken, string? debugSessionCert, bool? telemetryEnabled)
    {
        // Arrange
        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();
        var configurationBuilder = new ConfigurationBuilder();

        if (debugSessionPort is not null)
        {
            configurationBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>("DEBUG_SESSION_PORT", debugSessionPort)]);
        }

        if (debugSessionToken is not null)
        {
            configurationBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>("DEBUG_SESSION_TOKEN", debugSessionToken)]);
        }

        if (debugSessionCert is not null)
        {
            configurationBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>("DEBUG_SESSION_SERVER_CERTIFICATE", debugSessionCert)]);
        }

        var configuration = configurationBuilder.Build();
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            TelemetryOptOut = telemetryEnabled,
            DashboardPath = "test.dll",
            DashboardUrl = "http://localhost:8080",
            OtlpGrpcEndpointUrl = "http://localhost:4317"
        });
        var hook = CreateHook(resourceLoggerService, resourceNotificationService, configuration, dashboardOptions: dashboardOptions);

        var model = new DistributedApplicationModel(new ResourceCollection());

        // Act
        await hook.BeforeStartAsync(model, CancellationToken.None).DefaultTimeout();
        var dashboardResource = model.Resources.Single(r => string.Equals(r.Name, KnownResourceNames.AspireDashboard, StringComparisons.ResourceName));
        var context = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run) { ServiceProvider = TestServiceProvider.Instance });
        var dashboardEnvironmentVariables = new ConcurrentDictionary<string, string?>();
        await dashboardResource.ProcessEnvironmentVariableValuesAsync(context, (key, _, value, _) => dashboardEnvironmentVariables[key] = value, new FakeLogger()).DefaultTimeout();

        // Assert
        Assert.Equal(expectedDebugSessionPort?.ToString(), dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionPortName.EnvVarName));
        Assert.Equal(debugSessionToken, dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionTokenName.EnvVarName));
        Assert.Equal(debugSessionCert, dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionServerCertificateName.EnvVarName));
        Assert.Equal(telemetryEnabled, bool.TryParse(dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionTelemetryOptOutName.EnvVarName, null), out var b) ? b : null);
    }

    [Theory]
    [InlineData("http://localhost", "1234", "cert", true)]
    [InlineData("http://localhost", "1234", "cert", false)]
    [InlineData(null, null, null, null)]
    public async Task BeforeStartAsync_DashboardContainsDebugSessionInfo(string? debugSessionAddress, string? debugSessionToken, string? debugSessionCert, bool? telemetryEnabled)
    {
        // Arrange
        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();
        var configurationBuilder = new ConfigurationBuilder();

        if (debugSessionAddress is not null)
        {
            configurationBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>("DEBUG_SESSION_PORT", debugSessionAddress)]);
        }

        if (debugSessionToken is not null)
        {
            configurationBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>("DEBUG_SESSION_TOKEN", debugSessionToken)]);
        }

        if (debugSessionCert is not null)
        {
            configurationBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>("DEBUG_SESSION_SERVER_CERTIFICATE", debugSessionCert)]);
        }

        var configuration = configurationBuilder.Build();
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            TelemetryOptOut = telemetryEnabled,
            DashboardPath = "test.dll",
            DashboardUrl = "http://localhost:8080",
            OtlpGrpcEndpointUrl = "http://localhost:4317"
        });
        var hook = CreateHook(resourceLoggerService, resourceNotificationService, configuration, dashboardOptions: dashboardOptions);

        var model = new DistributedApplicationModel(new ResourceCollection());

        // Act
        await hook.BeforeStartAsync(model, CancellationToken.None).DefaultTimeout();
        var dashboardResource = model.Resources.Single(r => string.Equals(r.Name, KnownResourceNames.AspireDashboard, StringComparisons.ResourceName));
        var context = new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run) { ServiceProvider = TestServiceProvider.Instance });
        var dashboardEnvironmentVariables = new ConcurrentDictionary<string, string?>();
        await dashboardResource.ProcessEnvironmentVariableValuesAsync(context, (key, _, value, _) => dashboardEnvironmentVariables[key] = value, new FakeLogger()).DefaultTimeout();

        // Assert
        Assert.Equal(debugSessionAddress, dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionAddressName.EnvVarName));
        Assert.Equal(debugSessionToken, dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionTokenName.EnvVarName));
        Assert.Equal(debugSessionCert, dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionServerCertificateName.EnvVarName));
        Assert.Equal(telemetryEnabled, bool.TryParse(dashboardEnvironmentVariables.GetValueOrDefault(DashboardConfigNames.DebugSessionTelemetryOptOutName.EnvVarName, null), out var b) ? b : null);
    }

    [Fact]
    public async Task ConfigureEnvironmentVariables_HasAspireDashboardEnvVars_CopiedToDashboard()
    {
        // Arrange
        var resourceLoggerService = new ResourceLoggerService();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ASPIRE_DASHBOARD_PURPLE_MONKEY_DISHWASHER", "true" }
        });
        var configuration = configurationBuilder.Build();
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardPath = "test.dll",
            DashboardUrl = "http://localhost:8080",
            OtlpGrpcEndpointUrl = "http://localhost:4317",
        });
        var hook = CreateHook(resourceLoggerService, resourceNotificationService, configuration, dashboardOptions: dashboardOptions);

        var envVars = new Dictionary<string, object>();

        // Act
        await hook.ConfigureEnvironmentVariables(new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), environmentVariables: envVars));

        // Assert
        Assert.Equal("true", envVars.Single(e => e.Key == "ASPIRE_DASHBOARD_PURPLE_MONKEY_DISHWASHER").Value);
    }

    private static DashboardLifecycleHook CreateHook(
        ResourceLoggerService resourceLoggerService,
        ResourceNotificationService resourceNotificationService,
        IConfiguration configuration,
        ILoggerFactory? loggerFactory = null,
        IOptions<CodespacesOptions>? codespacesOptions = null,
        IOptions<DevcontainersOptions>? devcontainersOptions = null,
        IOptions<DashboardOptions>? dashboardOptions = null
        )
    {
        codespacesOptions ??= Options.Create(new CodespacesOptions());
        devcontainersOptions ??= Options.Create(new DevcontainersOptions());
        dashboardOptions ??= Options.Create(new DashboardOptions { DashboardPath = "test.dll" });
        var settingsWriter = new DevcontainerSettingsWriter(NullLogger<DevcontainerSettingsWriter>.Instance, codespacesOptions, devcontainersOptions);
        var rewriter = new CodespacesUrlRewriter(codespacesOptions);

        return new DashboardLifecycleHook(
            configuration,
            dashboardOptions,
            NullLogger<DistributedApplication>.Instance,
            new TestDashboardEndpointProvider(),
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resourceNotificationService,
            resourceLoggerService,
            loggerFactory ?? NullLoggerFactory.Instance,
            new DcpNameGenerator(configuration, Options.Create(new DcpOptions())),
            new TestHostApplicationLifetime(),
            rewriter,
            codespacesOptions,
            devcontainersOptions,
            settingsWriter
            );
    }

    public static IEnumerable<object?[]> Data()
    {
        var timestamp = new DateTime(2001, 12, 29, 23, 59, 59, DateTimeKind.Utc);
        var message = new DashboardLogMessage
        {
            LogLevel = LogLevel.Error,
            Category = "TestCategory",
            Message = "Hello world",
            Timestamp = timestamp.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.InvariantCulture),
        };
        var messageJson = JsonSerializer.Serialize(message, DashboardLogMessageContext.Default.DashboardLogMessage);

        yield return new object?[]
        {
            DateTime.UtcNow,
            messageJson,
            "Hello world",
            "Aspire.Hosting.Dashboard.TestCategory",
            LogLevel.Error
        };
        yield return new object?[]
        {
            null,
            messageJson,
            "Hello world",
            "Aspire.Hosting.Dashboard.TestCategory",
            LogLevel.Error
        };

        message = new DashboardLogMessage
        {
            LogLevel = LogLevel.Critical,
            Category = "TestCategory.TestSubCategory",
            Message = "Error message",
            Exception = new InvalidOperationException("Error!").ToString(),
            Timestamp = timestamp.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.InvariantCulture),
        };
        messageJson = JsonSerializer.Serialize(message, DashboardLogMessageContext.Default.DashboardLogMessage);

        yield return new object?[]
        {
            null,
            messageJson,
            $"Error message{Environment.NewLine}System.InvalidOperationException: Error!",
            "Aspire.Hosting.Dashboard.TestCategory.TestSubCategory",
            LogLevel.Critical
        };
    }

    private sealed class TestDashboardEndpointProvider : IDashboardEndpointProvider
    {
        public Task<string> GetResourceServiceUriAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("http://localhost:1010");
        }
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted { get; }
        public CancellationToken ApplicationStopped { get; }
        public CancellationToken ApplicationStopping { get; }

        public void StopApplication()
        {
        }
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public static IServiceProvider Instance { get; } = new TestServiceProvider();

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(DistributedApplicationModel))
            {
                return new DistributedApplicationModel(new ResourceCollection());
            }

            throw new ArgumentOutOfRangeException(nameof(serviceType));
        }
    }
}
