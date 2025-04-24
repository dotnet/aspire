// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Stress.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(TraceCreator.ActivitySourceName, ProducerConsumer.ActivitySourceName)
        .AddSource("Services.Api"))
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

app.MapGet("/overflow-counter", (TestMetrics metrics) =>
{
    // Emit measurements to ensure at least 2000 unique tag values are emitted,
    // matching the default cardinality limit in OpenTelemetry.
    for (var i = 0; i < 250; i++)
    {
        for (int j = 0; j < 10; j++)
        {
            metrics.IncrementCounter(1, new TagList([new KeyValuePair<string, object?>($"add-tag-{i}", j.ToString(CultureInfo.InvariantCulture))]));
        }
    }

    return "Counter overflowed";
});

app.MapGet("/big-trace", async () =>
{
    var traceCreator = new TraceCreator
    {
        IncludeBrokenLinks = true
    };

    await traceCreator.CreateTraceAsync("bigtrace", count: 10, createChildren: true);

    return "Big trace created";
});

app.MapGet("/trace-limit", async () =>
{
    const int TraceCount = 20_000;

    var current = Activity.Current;
    Activity.Current = null;
    var traceCreator = new TraceCreator();

    for (var i = 0; i < TraceCount; i++)
    {
        // Delay so OTEL has the opportunity to send traces.
        if (i % 1000 == 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
        }

        await traceCreator.CreateTraceAsync($"tracelimit-{i}", count: 1, createChildren: false);
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

app.MapGet("/log-message-limit", async ([FromServices] ILogger<Program> logger) =>
{
    const int LogCount = 10_000;
    const int BatchSize = 100;

    for (var i = 0; i < LogCount / BatchSize; i++)
    {
        for (var j = 0; j < BatchSize; j++)
        {
            logger.LogInformation("Log entry {BatchIndex}-{LogEntryIndex}", i, j);
        }

        await Task.Delay(100);
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

    var xmlWithUrl = new XElement(new XElement("url", "http://localhost:8080")).ToString();

    // From https://microsoftedge.github.io/Demos/json-dummy-data/
    var jsonLarge = File.ReadAllText(Path.Combine("content", "example.json"));

    var jsonWithComments = @"
// line comment
[
    /* block comment */
    1
]";

    var jsonWithUrl = new JsonObject
    {
        ["url"] = "http://localhost:8080"
    }.ToString();

    var sb = new StringBuilder();
    for (int i = 0; i < 26; i++)
    {
        var line = new string((char)('a' + i), 256);
        sb.AppendLine(line);
    }

    logger.LogInformation(@"XML large content: {XmlLarge}
XML comment content: {XmlComment}
XML URL content: {XmlUrl}
JSON large content: {JsonLarge}
JSON comment content: {JsonComment}
JSON URL content: {JsonUrl}
Long line content: {LongLines}
URL content: {UrlContent}
Empty content: {EmptyContent}
Whitespace content: {WhitespaceContent}
Null content: {NullContent}", xmlLarge, xmlWithComments, xmlWithUrl, jsonLarge, jsonWithComments, jsonWithUrl, sb.ToString(), "http://localhost:8080", "", "   ", null);

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

app.MapGet("/multiple-traces-linked", async () =>
{
    const int TraceCount = 2;

    var current = Activity.Current;
    Activity.Current = null;
    var traceCreator = new TraceCreator();

    await traceCreator.CreateTraceAsync("trace1", count: 1, createChildren: true, rootName: "LinkedTrace1");
    await traceCreator.CreateTraceAsync("trace2", count: 1, createChildren: true, rootName: "LinkedTrace2");
    await traceCreator.CreateTraceAsync("trace3", count: 1, createChildren: true, rootName: "LinkedTrace3");

    Activity.Current = current;

    return $"Created {TraceCount} traces.";
});

app.MapGet("/nested-trace-spans", async () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    "Sample Text"
                ))
            .ToArray();
        ActivitySource source = new("Services.Api", "1.0.0");
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        });
        using var activity = source.StartActivity("ValidateAndUpdateCacheService.ExecuteAsync");
        await Task.Delay(100);
        Debug.Assert(activity is not null);
        using var innerActivity = source.StartActivity("ValidateAndUpdateCacheService.activeUser",
            ActivityKind.Internal, parentContext: activity.Context);
        await Task.Delay(100);
        Debug.Assert(innerActivity is not null);
        using (source.StartActivity("Perform1", ActivityKind.Internal, parentContext: innerActivity.Context))
        {
            await Task.Delay(10);
        }

        using (source.StartActivity("Perform2", ActivityKind.Internal, parentContext: innerActivity.Context))
        {
            await Task.Delay(20);
        }

        using (source.StartActivity("Perform3", ActivityKind.Internal, parentContext: innerActivity.Context))
        {
            await Task.Delay(30);
        }

        using var innerActivity2 = source.StartActivity("ValidateAndUpdateCacheService.activeUser",
            ActivityKind.Internal, parentContext: activity.Context);
        await Task.Delay(100);
        Debug.Assert(innerActivity2 is not null);

        using (source.StartActivity("Perform1", ActivityKind.Internal, parentContext: innerActivity2.Context))
        {
            await Task.Delay(30);
        }

        using (source.StartActivity("Perform2", ActivityKind.Internal, parentContext: innerActivity2.Context))
        {
            await Task.Delay(20);
        }

        using (source.StartActivity("Perform3", ActivityKind.Internal, parentContext: innerActivity2.Context))
        {
            await Task.Delay(10);
        }

        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

public record WeatherForecast(DateOnly Date, int TemperatureC, string Summary);
