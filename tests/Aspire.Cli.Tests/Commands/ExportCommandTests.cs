// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Otlp;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Otlp.Serialization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aspire.Cli.Tests.Commands;

public class ExportCommandTests(ITestOutputHelper outputHelper)
{
    private static readonly DateTime s_testTime = TelemetryTestHelper.s_testTime;

    [Fact]
    public async Task ExportCommand_WritesZipWithExpectedData()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var outputPath = Path.Combine(workspace.WorkspaceRoot.FullName, "export.zip");

        var resources = new[]
        {
            new ResourceInfoJson { Name = "redis", InstanceId = null },
            new ResourceInfoJson { Name = "apiservice", InstanceId = null },
        };

        var logsJson = BuildLogsJson(
            ("redis", null, 9, "Information", "Ready to accept connections", s_testTime),
            ("apiservice", null, 9, "Information", "Request received", s_testTime.AddSeconds(1)));

        var tracesJson = BuildTracesJson(
            ("apiservice", null, "span001", "GET /api/products", s_testTime, s_testTime.AddMilliseconds(50), false));

        var provider = CreateExportTestServices(workspace, outputWriter, resources,
            telemetryEndpoints: new Dictionary<string, string>
            {
                ["/api/telemetry/logs"] = logsJson,
                ["/api/telemetry/traces"] = tracesJson,
            },
            resourceSnapshots:
            [
                new ResourceSnapshot { Name = "redis", DisplayName = "redis", ResourceType = "Container", State = "Running" },
                new ResourceSnapshot { Name = "apiservice", DisplayName = "apiservice", ResourceType = "Project", State = "Running" },
            ],
            logLines:
            [
                new ResourceLogLine { ResourceName = "redis", LineNumber = 1, Content = "Redis is starting" },
                new ResourceLogLine { ResourceName = "redis", LineNumber = 2, Content = "Ready to accept connections" },
                new ResourceLogLine { ResourceName = "apiservice", LineNumber = 1, Content = "Now listening on: https://localhost:5001" },
            ]);

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"export --output {outputPath}");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.True(File.Exists(outputPath), "Export zip file should be created");

        using var archive = ZipFile.OpenRead(outputPath);
        var entryNames = archive.Entries.Select(e => e.FullName).OrderBy(n => n).ToList();

        Assert.Collection(entryNames,
            entry => Assert.Equal("consolelogs/apiservice.txt", entry),
            entry => Assert.Equal("consolelogs/redis.txt", entry),
            entry => Assert.Equal("resources/apiservice.json", entry),
            entry => Assert.Equal("resources/redis.json", entry),
            entry => Assert.Equal("structuredlogs/structured-logs.json", entry),
            entry => Assert.Equal("traces/traces.json", entry));

        // Verify console log content
        var redisConsoleLog = ReadEntryText(archive, "consolelogs/redis.txt");
        Assert.Contains("Redis is starting", redisConsoleLog);
        Assert.Contains("Ready to accept connections", redisConsoleLog);

        var apiConsoleLog = ReadEntryText(archive, "consolelogs/apiservice.txt");
        Assert.Contains("Now listening on: https://localhost:5001", apiConsoleLog);

        // Verify resource JSON content
        var redisResourceJson = ReadEntryText(archive, "resources/redis.json");
        Assert.Contains("redis", redisResourceJson);
        Assert.Contains("Container", redisResourceJson);

        // Verify structured logs content is valid JSON
        var structuredLogsJson = ReadEntryText(archive, "structuredlogs/structured-logs.json");
        var structuredLogsData = JsonSerializer.Deserialize(structuredLogsJson, OtlpJsonSerializerContext.Default.OtlpTelemetryDataJson);
        Assert.NotNull(structuredLogsData?.ResourceLogs);
        Assert.NotEmpty(structuredLogsData.ResourceLogs);

        // Verify traces content is valid JSON
        var tracesContent = ReadEntryText(archive, "traces/traces.json");
        var tracesData = JsonSerializer.Deserialize(tracesContent, OtlpJsonSerializerContext.Default.OtlpTelemetryDataJson);
        Assert.NotNull(tracesData?.ResourceSpans);
        Assert.NotEmpty(tracesData.ResourceSpans);
    }

    [Fact]
    public async Task ExportCommand_OutputOption_ConfiguresArchiveOutputLocation()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var customDir = Path.Combine(workspace.WorkspaceRoot.FullName, "custom", "nested");
        var outputPath = Path.Combine(customDir, "my-export.zip");

        var provider = CreateExportTestServices(workspace, outputWriter,
            resources: [new ResourceInfoJson { Name = "redis", InstanceId = null }],
            telemetryEndpoints: new Dictionary<string, string>
            {
                ["/api/telemetry/logs"] = BuildLogsJson(),
                ["/api/telemetry/traces"] = BuildTracesJson(),
            },
            resourceSnapshots:
            [
                new ResourceSnapshot { Name = "redis", DisplayName = "redis", ResourceType = "Container", State = "Running" },
            ],
            logLines: []);

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"export --output {outputPath}");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.True(File.Exists(outputPath), $"Export zip file should be created at the specified path: {outputPath}");
        Assert.True(Directory.Exists(customDir), "Nested output directory should be created automatically");
    }

    [Fact]
    public async Task ExportCommand_AppHostOption_UsesSpecifiedAppHost()
    {
        // When --apphost is specified, AppHostConnectionResolver uses a fast path
        // that looks for matching socket files on disk. Since no real socket exists
        // in tests, the command gracefully reports that no running AppHost was found.
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var outputPath = Path.Combine(workspace.WorkspaceRoot.FullName, "export.zip");

        // Create the apphost project file on disk so the FileInfo option resolves
        var appHostDir = Path.Combine(workspace.WorkspaceRoot.FullName, "MyAppHost");
        Directory.CreateDirectory(appHostDir);
        var appHostProjectPath = Path.Combine(appHostDir, "MyAppHost.csproj");
        File.WriteAllText(appHostProjectPath, "<Project />");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.OutputTextWriter = outputWriter;
            options.DisableAnsi = true;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"export --apphost {appHostProjectPath} --output {outputPath}");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // The command succeeds but displays "not found" because there is no
        // socket file for the specified apphost.
        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.False(File.Exists(outputPath), "No zip should be created when the AppHost is not running");
    }

    [Fact]
    public async Task ExportCommand_SingleInScopeConnection_ExportsCorrectData()
    {
        // When --apphost is NOT specified and only one in-scope connection exists,
        // the resolver automatically selects it and exports data from that connection.
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var outputWriter = new TestOutputTextWriter(outputHelper);
        var outputPath = Path.Combine(workspace.WorkspaceRoot.FullName, "export.zip");

        var monitor = new TestAuxiliaryBackchannelMonitor();

        // Connection 1 – out-of-scope, should NOT be used
        var outOfScopeConnection = new TestAppHostAuxiliaryBackchannel
        {
            IsInScope = false,
            AppHostInfo = new AppHostInformation
            {
                AppHostPath = Path.Combine("C:", "other", "OtherAppHost", "OtherAppHost.csproj"),
                ProcessId = 1111
            },
            DashboardInfoResponse = new GetDashboardInfoResponse
            {
                ApiBaseUrl = "http://localhost:19999",
                ApiToken = "other-token",
                DashboardUrls = ["http://localhost:19999/login?t=other"],
                IsHealthy = true
            },
            ResourceSnapshots =
            [
                new ResourceSnapshot { Name = "other-resource", DisplayName = "other-resource", ResourceType = "Project", State = "Running" },
            ]
        };
        monitor.AddConnection("hash-other", "socket.hash-other", outOfScopeConnection);

        // Connection 2 – the only in-scope connection, should be auto-selected
        var targetConnection = new TestAppHostAuxiliaryBackchannel
        {
            IsInScope = true,
            AppHostInfo = new AppHostInformation
            {
                AppHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "TargetAppHost", "TargetAppHost.csproj"),
                ProcessId = 2222
            },
            DashboardInfoResponse = new GetDashboardInfoResponse
            {
                ApiBaseUrl = "http://localhost:18888",
                ApiToken = "test-token",
                DashboardUrls = ["http://localhost:18888/login?t=test"],
                IsHealthy = true
            },
            ResourceSnapshots =
            [
                new ResourceSnapshot { Name = "target-resource", DisplayName = "target-resource", ResourceType = "Container", State = "Running" },
            ],
            LogLines =
            [
                new ResourceLogLine { ResourceName = "target-resource", LineNumber = 1, Content = "Target resource log" },
            ]
        };
        monitor.AddConnection("hash-target", "socket.hash-target", targetConnection);

        var resourcesJson = JsonSerializer.Serialize(
            new[] { new ResourceInfoJson { Name = "target-resource", InstanceId = null } },
            OtlpJsonSerializerContext.Default.ResourceInfoJsonArray);

        var handler = new MockHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("/api/telemetry/resources"))
            {
                return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(resourcesJson, System.Text.Encoding.UTF8, "application/json")
                };
            }
            if (url.Contains("/api/telemetry/logs"))
            {
                return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildLogsJson(), System.Text.Encoding.UTF8, "application/json")
                };
            }
            if (url.Contains("/api/telemetry/traces"))
            {
                return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildTracesJson(), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
            options.OutputTextWriter = outputWriter;
            options.DisableAnsi = true;
        });

        services.AddSingleton(handler);
        services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new MockHttpClientFactory(handler)));

        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"export --output {outputPath}");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.True(File.Exists(outputPath), "Export zip file should be created");

        using var archive = ZipFile.OpenRead(outputPath);
        var entryNames = archive.Entries.Select(e => e.FullName).Order().ToList();

        Assert.Collection(entryNames,
            entry => Assert.Equal("consolelogs/target-resource.txt", entry),
            entry => Assert.Equal("resources/target-resource.json", entry));
        var logContent = ReadEntryText(archive, "consolelogs/target-resource.txt");
        Assert.Contains("Target resource log", logContent);
    }

    /// <summary>
    /// Creates a configured <see cref="ServiceProvider"/> for export command tests,
    /// with a mock backchannel and HTTP handler that serves resource and telemetry data.
    /// </summary>
    private ServiceProvider CreateExportTestServices(
        TemporaryWorkspace workspace,
        TestOutputTextWriter outputWriter,
        ResourceInfoJson[] resources,
        Dictionary<string, string> telemetryEndpoints,
        List<ResourceSnapshot> resourceSnapshots,
        List<ResourceLogLine> logLines)
    {
        var resourcesJson = JsonSerializer.Serialize(resources, OtlpJsonSerializerContext.Default.ResourceInfoJsonArray);

        var monitor = new TestAuxiliaryBackchannelMonitor();
        var connection = new TestAppHostAuxiliaryBackchannel
        {
            IsInScope = true,
            AppHostInfo = new AppHostInformation
            {
                AppHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "TestAppHost", "TestAppHost.csproj"),
                ProcessId = 1234
            },
            DashboardInfoResponse = new GetDashboardInfoResponse
            {
                ApiBaseUrl = "http://localhost:18888",
                ApiToken = "test-token",
                DashboardUrls = ["http://localhost:18888/login?t=test"],
                IsHealthy = true
            },
            ResourceSnapshots = resourceSnapshots,
            LogLines = logLines
        };
        monitor.AddConnection("hash1", "socket.hash1", connection);

        var handler = new MockHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("/api/telemetry/resources"))
            {
                return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(resourcesJson, System.Text.Encoding.UTF8, "application/json")
                };
            }

            foreach (var (urlPattern, json) in telemetryEndpoints)
            {
                if (url.Contains(urlPattern))
                {
                    return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }

            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.AuxiliaryBackchannelMonitorFactory = _ => monitor;
            options.OutputTextWriter = outputWriter;
            options.DisableAnsi = true;
        });

        services.AddSingleton(handler);
        services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new MockHttpClientFactory(handler)));

        return services.BuildServiceProvider();
    }

    private static string ReadEntryText(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        Assert.NotNull(entry);
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string BuildLogsJson(params (string serviceName, string? instanceId, int severityNumber, string severityText, string body, DateTime time)[] entries)
    {
        if (entries.Length == 0)
        {
            var emptyResponse = new TelemetryApiResponse
            {
                Data = new OtlpTelemetryDataJson { ResourceLogs = [] },
                TotalCount = 0,
                ReturnedCount = 0
            };
            return JsonSerializer.Serialize(emptyResponse, OtlpJsonSerializerContext.Default.TelemetryApiResponse);
        }

        var resourceLogs = entries
            .GroupBy(e => (e.serviceName, e.instanceId))
            .Select(g => new OtlpResourceLogsJson
            {
                Resource = TelemetryTestHelper.CreateOtlpResource(g.Key.serviceName, g.Key.instanceId),
                ScopeLogs =
                [
                    new OtlpScopeLogsJson
                    {
                        LogRecords = g.Select(e => new OtlpLogRecordJson
                        {
                            TimeUnixNano = TelemetryTestHelper.DateTimeToUnixNanoseconds(e.time),
                            SeverityNumber = e.severityNumber,
                            SeverityText = e.severityText,
                            Body = new OtlpAnyValueJson { StringValue = e.body }
                        }).ToArray()
                    }
                ]
            }).ToArray();

        var response = new TelemetryApiResponse
        {
            Data = new OtlpTelemetryDataJson { ResourceLogs = resourceLogs },
            TotalCount = entries.Length,
            ReturnedCount = entries.Length
        };

        return JsonSerializer.Serialize(response, OtlpJsonSerializerContext.Default.TelemetryApiResponse);
    }

    private static string BuildTracesJson(params (string serviceName, string? instanceId, string spanId, string name, DateTime startTime, DateTime endTime, bool hasError)[] entries)
    {
        if (entries.Length == 0)
        {
            var emptyResponse = new TelemetryApiResponse
            {
                Data = new OtlpTelemetryDataJson { ResourceSpans = [] },
                TotalCount = 0,
                ReturnedCount = 0
            };
            return JsonSerializer.Serialize(emptyResponse, OtlpJsonSerializerContext.Default.TelemetryApiResponse);
        }

        var resourceSpans = entries
            .GroupBy(e => (e.serviceName, e.instanceId))
            .Select(g => new OtlpResourceSpansJson
            {
                Resource = TelemetryTestHelper.CreateOtlpResource(g.Key.serviceName, g.Key.instanceId),
                ScopeSpans =
                [
                    new OtlpScopeSpansJson
                    {
                        Spans = g.Select(e => new OtlpSpanJson
                        {
                            SpanId = e.spanId,
                            TraceId = "trace001",
                            Name = e.name,
                            StartTimeUnixNano = TelemetryTestHelper.DateTimeToUnixNanoseconds(e.startTime),
                            EndTimeUnixNano = TelemetryTestHelper.DateTimeToUnixNanoseconds(e.endTime),
                            Status = e.hasError ? new OtlpSpanStatusJson { Code = 2 } : new OtlpSpanStatusJson { Code = 1 }
                        }).ToArray()
                    }
                ]
            }).ToArray();

        var response = new TelemetryApiResponse
        {
            Data = new OtlpTelemetryDataJson { ResourceSpans = resourceSpans },
            TotalCount = entries.Length,
            ReturnedCount = entries.Length
        };

        return JsonSerializer.Serialize(response, OtlpJsonSerializerContext.Default.TelemetryApiResponse);
    }
}
