// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using Stress.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(TraceCreator.ActivitySourceName, ProducerConsumer.ActivitySourceName))
    .WithMetrics(metrics => metrics.AddMeter(TestMetrics.MeterName));
builder.Services.AddSingleton<TestMetrics>();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(ConsoleStresser.Stress);

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = app.Services.GetRequiredService<TestMetrics>();
});

app.MapGet("/", () => "Hello world");

app.MapGet("/write-console", () =>
{
    for (var i = 0; i < 5000; i++)
    {
        if (i % 500 == 0)
        {
            Console.Error.WriteLine($"{i} Error");
        }
        else
        {
            Console.Out.WriteLine($"{i} Out");
        }
    }

    return "Console written";
});

app.MapGet("/increment-counter", (TestMetrics metrics) =>
{
    metrics.IncrementCounter(1, new TagList([new KeyValuePair<string, object?>("add-tag", "1")]));
    metrics.IncrementCounter(2, new TagList([new KeyValuePair<string, object?>("add-tag", "")]));
    metrics.IncrementCounter(3, default);

    return "Counter incremented";
});

app.MapGet("/big-trace", async () =>
{
    var bigTraceCreator = new TraceCreator();

    await bigTraceCreator.CreateTraceAsync(count: 10, createChildren: true);

    return "Big trace created";
});

app.MapGet("/trace-limit", async () =>
{
    const int TraceCount = 20_000;

    var current = Activity.Current;
    Activity.Current = null;
    var bigTraceCreator = new TraceCreator();

    for (var i = 0; i < TraceCount; i++)
    {
        await bigTraceCreator.CreateTraceAsync(count: 1, createChildren: false);
    }

    Activity.Current = current;

    return $"Created {TraceCount} traces.";
});

app.MapGet("/http-client-requests", async (HttpClient client) =>
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")!.Split(';');

    foreach (var url in urls)
    {
        var response = await client.GetAsync(url);
        await response.Content.ReadAsStringAsync();
    }

    return $"Sent requests to {string.Join(';', urls)}";
});

app.MapGet("/log-message-limit", ([FromServices] ILogger<Program> logger) =>
{
    const int LogCount = 20_000;

    for (var i = 0; i < LogCount; i++)
    {
        logger.LogInformation("Log entry {LogEntryIndex}", i);
    }

    return $"Created {LogCount} logs.";
});

app.MapGet("/log-message", ([FromServices] ILogger<Program> logger) =>
{
    const string message = "Hello World";
    IReadOnlyList<KeyValuePair<string, object?>> eventData = new List<KeyValuePair<string, object?>>()
    {
        new KeyValuePair<string, object?>("Message", message),
        new KeyValuePair<string, object?>("ActivityId", 123),
        new KeyValuePair<string, object?>("Level", (int) 10),
        new KeyValuePair<string, object?>("Tid", Environment.CurrentManagedThreadId),
        new KeyValuePair<string, object?>("Pid", Environment.ProcessId),
    };

    logger.Log(LogLevel.Information, 0, eventData, null, formatter: (_, _) => null!);

    return message;
});

app.MapGet("/many-logs", (ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
{
    var channel = Channel.CreateUnbounded<string>();
    var logger = loggerFactory.CreateLogger("ManyLogs");

    cancellationToken.Register(() =>
    {
        logger.LogInformation("Writing logs canceled.");
    });

    // Write logs for 1 minute.
    _ = Task.Run(async () =>
    {
        var stopwatch = Stopwatch.StartNew();
        var logCount = 0;
        while (stopwatch.Elapsed < TimeSpan.FromMinutes(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            logCount++;
            logger.LogInformation("This is log message {LogCount}.", logCount);

            if (logCount % 100 == 0)
            {
                channel.Writer.TryWrite($"Logged {logCount} messages.");
            }

            await Task.Delay(5, cancellationToken);
        }

        channel.Writer.Complete();
    }, cancellationToken);

    return WriteOutput();

    async IAsyncEnumerable<string> WriteOutput()
    {
        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }
});

app.MapGet("/producer-consumer", async () =>
{
    var producerConsumer = new ProducerConsumer();

    await producerConsumer.ProduceAndConsumeAsync(count: 5);

    return "Produced and consumed";
});

app.MapGet("/log-formatting", (ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("LogAttributes");

    // From https://learn.microsoft.com/previous-versions/windows/desktop/ms762271(v=vs.85)
    var xmlLarge = File.ReadAllText(Path.Combine("content", "books.xml"));

    var xmlWithComments = @"<hello><!-- world --></hello>";

    // From https://microsoftedge.github.io/Demos/json-dummy-data/
    var jsonLarge = File.ReadAllText(Path.Combine("content", "example.json"));

    var jsonWithComments = @"
// line comment
[
    /* block comment */
    1
]";

    var sb = new StringBuilder();
    for (int i = 0; i < 26; i++)
    {
        var line = new string((char)('a' + i), 256);
        sb.AppendLine(line);
    }

    logger.LogInformation(@"XML large content: {XmlLarge}
XML comment content: {XmlComment}
JSON large content: {JsonLarge}
JSON comment content: {JsonComment}
Long line content: {LongLines}", xmlLarge, xmlWithComments, jsonLarge, jsonWithComments, sb.ToString());

    return "Log with formatted data";
});

app.MapGet("/duplicate-spanid", async () =>
{
    var traceCreator = new TraceCreator();

    var span1 = traceCreator.CreateActivity("Test 1", "0485b1947fe788bb");
    await Task.Delay(1000);
    span1?.Stop();

    var span2 = traceCreator.CreateActivity("Test 2", "0485b1947fe788bb");
    await Task.Delay(1000);
    span2?.Stop();

    return $"Created duplicate span IDs.";
});

app.MapGet("/log-entry-nobody", async (IConfiguration configuration) =>
{
    var channel = GrpcChannel.ForAddress(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!);

    var resource = new Resource();
    resource.Attributes.Add(new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = configuration["OTEL_SERVICE_NAME"]! } });

    foreach (var item in configuration["OTEL_RESOURCE_ATTRIBUTES"]!.Split(';'))
    {
        var headerParts = item.Split('=');

        resource.Attributes.Add(new KeyValue { Key = headerParts[0], Value = new AnyValue { StringValue = headerParts[1] } });
    }

    var metadata = new Metadata();
    foreach (var item in configuration["OTEL_EXPORTER_OTLP_HEADERS"]!.Split(';'))
    {
        var headerParts = item.Split('=');

        metadata.Add(headerParts[0], headerParts[1]);
    }

    var client = new LogsService.LogsServiceClient(channel);
    var response = await client.ExportAsync(new ExportLogsServiceRequest
    {
        ResourceLogs =
        {
            new ResourceLogs
            {
                Resource = resource,
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = new InstrumentationScope { Name = "NoBodySource" },
                        LogRecords =
                        {
                            new LogRecord
                            {
                            }
                        }
                    }
                }
            }
        }
    }, metadata);

    return response.PartialSuccess?.RejectedLogRecords > 0 ? "Failure" : "Success";
});

app.Run();
