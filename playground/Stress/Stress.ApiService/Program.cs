// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Stress.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(b =>
    {
        b.AddService(builder.Environment.ApplicationName);
    })
    .WithTracing(tracing => tracing
        .AddSource(TraceCreator.ActivitySourceName, ProducerConsumer.ActivitySourceName)
        .AddSource("Services.Api"))
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(TestMetrics.MeterName);
        metrics.SetExemplarFilter(ExemplarFilterType.AlwaysOn);

    });
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

app.MapGet("/write-console-large", () =>
{
    var random = new Random();

    for (var i = 0; i < 5000; i++)
    {
        var data = new byte[i];
        random.NextBytes(data);
        var payload = Convert.ToHexString(data);

        Console.Out.WriteLine($"{i} Out. Payload: {payload}");
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

app.MapGet("/exemplars-no-span", (TestMetrics metrics) =>
{
    var activity = Activity.Current;
    Activity.Current = null;

    metrics.RecordHistogram(Random.Shared.NextDouble(), new TagList());

    Activity.Current = activity;

    return "Exemplar recorded";
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

app.MapGet("/log-message-limit-large", async ([FromServices] ILogger<Program> logger) =>
{
    const int LogCount = 10_000;
    const int BatchSize = 100;

    var random = new Random();

    for (var i = 0; i < LogCount / BatchSize; i++)
    {
        for (var j = 0; j < BatchSize; j++)
        {
            var size = (i + 1) * (j + 1) / 10;
            var data = new byte[size];
            random.NextBytes(data);
            var payload = Convert.ToHexString(data);

            logger.LogInformation("Log entry {BatchIndex}-{LogEntryIndex}: {Payload}", i, j, payload);
        }

        await Task.Delay(50);
    }

    return $"Created {LogCount} logs.";

    static async Task RecurseToError(int current, int depth)
    {
        await Task.Yield();

        if (current == depth)
        {
            throw new InvalidOperationException($"Recursed to depth {depth}");
        }

        await RecurseToError(++current, depth);
    }
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

app.MapGet("/genai-trace", async () =>
{
    var source = new ActivitySource("Services.Api", "1.0.0");

    var activity = source.StartActivity("chat gpt", ActivityKind.Client);
    if (activity != null)
    {
        activity.SetTag("gen_ai.system", "gpt");
        activity.SetTag("gen_ai.input.messages", """
            [
              {
                "role": "user",
                "parts": [
                  {
                    "type": "text",
                    "content": "This is the input prompt."
                  },
                  {
                    "type": "uri",
                    "uri": "https://api.nuget.org/v3-flatcontainer/microsoft.extensions.dependencyinjection/9.0.9/icon",
                    "mime_type": "image/png",
                    "modality": "image"
                  },
                  {
                    "type": "blob",
                    "content": "iVBORw0KGgoAAAANSUhEUgAAAFwAAABcCAMAAADUMSJqAAAAilBMVEX///+XgOV0Vd1RK9S5qu7c1fZHGdKto+hOJ9Oag+ZNJNOWfuVyU91LINPi3Pi4qO6WiuOEbt9EEtJtTdtaNtZnRtn39f1WMdX6+f6ReeTs6fr08fwhAM68sO1iQtjIvvA8ANHl4fhePNeEaeHQx/Kjj+iwoOyeiednUtnCuO+jlOfWzvR5Xd5+Yt+aCjLkAAAD3ElEQVRoge2ZW3eqMBCFC0gM4VIJKlIPQkXbqvj//94hk1a5TChI+uZefes6X2dl78wMOS8vTz31VFPb7R+BN+nXv0pf6UY/295xalWifGfrZmcSDfg808sO7mxR/LtWeMSACj+WxSKd7BMX6DgyzSgWeH7Sx17vBTE2QbH4O/u1NviZCGAk4ZH4Q+Ssi73NxUEvzW8tBT3XdZ0K4Sb1fuBQOgv1sC/g5t68CRzgFy1wGUPvDvf0xTGFwldmTSsoPZ3OXse1GP4I4kinx/FYj2HDU3Kcyn5vu1mL4+QWU/gtN2ue+sU09gEK37XZprmD0g+T4BBpq8s2TeiS+ylTadaNYTOOs8fZa9JoKi1Pxe/I4y1mgcWwEUffeZQdEBlDz+ukRQj8IMGDcA+6YZkkYYnRZYspH2Nnoo0z0zEMw7lidIjjY6vABv7t0gC56LHHD8dxTuASOgB3Qqx0iCP5GM9+kxGX7EqlMo78bTQ8EU2FJT9sAy1dxjEZy7ZzmJu3wg0HTQzEMR+7PJoQQ9e4K1HH0RzHhhWLXZ0a3CmUno5bwGC20b3RUE8c4zFxhBXLD50GHI/j6AWsHUOtcQxFDGnSZuOeyjgOXsBgtsmm0lRfixk48TYQw9jtsHvj6A2Dw2xjRbfw3jjmgybeRm4qGLuSMo4WGRLHT2gqIQ7vi+Pid/Y7zLaV6yYuJqP0upILmP/7xIMVyz8f7MvpFdHsAzUa4Nff2IccZnpg23aQvc4QHbHTKgfFUYSWLm2pFKO/YqUb0GJ2/ew5hz0qkPALWvoZoTtwmnzex94yKu7DN9u2TygdvQFibFDat4AZ0A0z+yaMjXsaQhwNNTvgMBKDO/yAlv6Jle7B2FDHsRRNhdh1pYNLd1nvAgYrFjkHdTju6RHzFEpTLWAbmG07uyk07HgcxT1VTbwP4SZJgxYd9RSPoyidowvYmy9iWLbZCk8djA43kGETD2YbObTZYzxNmGLiBdBU3E7hlafowajjmHfjGIEbly5b5SlmqQHfeJ2JBysWPyOFC08xOhrHK0OeHDbw8hbhbJWn2MFAHK1mHBcyhji7amCD4xiyzgK2piKGhaLwSsNbzIq2X0tgDcJi2OupeuI1HgXE5fRDdeGKe4qWLmLX+E4SLzZEERUp9Cbh45S1Dl2szHIqK+HYuaCVm7QFFym/jWVMaBjR3uha7U8NWMj9wg4Uwk4lnX06Xbny27TRvKBr+XGyQGWEiApsa4wsZFt/g8d3ynxcDBPFBM2l/bKWiYPRI8o7o+6Q+3rYfpddnUzCOZksThL822ubzScr+6v/pnrqqad06z+o/mHi4pLvAgAAAABJRU5ErkJggg==",
                    "mime_type": "image/png",
                    "modality": "image"
                  }
                ]
              },
              {
                "role": "assistant",
                "parts": [
                  {
                    "type": "text",
                    "content": "Assistant content"
                  }
                ]
              },
              {
                "role": "user",
                "parts": [
                  {
                    "type": "text",
                    "content": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBB Lorem ipsum dolor sit amet consectetur adipiscing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat. In id cursus mi pretium tellus duis convallis. Tempus leo eu aenean sed diam urna tempor. Pulvinar vivamus fringilla lacus nec metus bibendum egestas. Iaculis massa nisl malesuada lacinia integer nunc posuere. Ut hendrerit semper vel class aptent taciti sociosqu. Ad litora torquent per conubia nostra inceptos himenaeos.\n\nLorem ipsum dolor sit amet consectetur adipiscing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat. In id cursus mi pretium tellus duis convallis. Tempus leo eu aenean sed diam urna tempor. Pulvinar vivamus fringilla lacus nec metus bibendum egestas. Iaculis massa nisl malesuada lacinia integer nunc posuere. Ut hendrerit semper vel class aptent taciti sociosqu. Ad litora torquent per conubia nostra inceptos himenaeos.\n\nLorem ipsum dolor sit amet consectetur adipiscing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat. In id cursus mi pretium tellus duis convallis. Tempus leo eu aenean sed diam urna tempor. Pulvinar vivamus fringilla lacus nec metus bibendum egestas. Iaculis massa nisl malesuada lacinia integer nunc posuere. Ut hendrerit semper vel class aptent taciti sociosqu. Ad litora torquent per conubia nostra inceptos himenaeos.\n\nLorem ipsum dolor sit amet consectetur adipiscing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat. In id cursus mi pretium tellus duis convallis. Tempus leo eu aenean sed diam urna tempor."
                  },
                  {
                    "type": "text",
                    "content": "# 📝 Markdown Feature Showcase\n\nWelcome to a **comprehensive example** of markdown in action.  \nThis document demonstrates *all* the main features.\n\n---\n\n## 1. Headings\n\n# H1 Heading  \n## H2 Heading  \n### H3 Heading  \n#### H4 Heading  \n##### H5 Heading  \n###### H6 Heading  \n\n---\n\n## 2. Emphasis\n\n- *Italic text*  \n- **Bold text**  \n- ***Bold and italic***  \n- ~~Strikethrough~~  \n- <u>Underlined (via HTML)</u>  \n\n---\n\n## 3. Lists\n\n### Unordered list:\n- Item A\n  - Sub-item A1\n  - Sub-item A2\n- Item B  \n- Item C  \n\n### Ordered list:\n1. First\n2. Second\n   1. Sub-second\n   2. Sub-second again\n3. Third  \n\n### Task list:\n- [x] Done item  \n- [ ] Pending item  \n- [ ] Another pending item  \n\n---\n\n## 4. Links\n\n- Inline link: [OpenAI](https://openai.com)  \n- Reference link: [Search Engine][google]  \n- Autolink: <https://example.com>  \n\n[google]: https://google.com \"Google Search\"\n\n---\n\n## 5. Images\n\nInline image:  \n![Example](/img/TokenExample.png)  \n\nLinked image:  \n[![Example](/img/TokenExample.png)](https://openai.com)\n\n---\n\n## 6. Blockquotes\n\n> This is a blockquote.  \n>  \n> > Nested blockquote inside.  \n\n---\n\n## 7. Horizontal Rules\n\n---  \n***  \n___  \n\n---\n\n## 8. Tables\n\n| Feature        | Supported | Notes                          |\n|----------------|-----------|--------------------------------|\n| **Bold**       | ✅        | Works inside tables too        |\n| *Italics*      | ✅        | Styling works fine             |\n| Links          | ✅        | [Example](https://openai.com)  |\n| Images         | ✅        | ![Img](/img/TokenExample.png) |\n| Task List      | ❌        | Not supported in table cells   |\n\n---\n\n## 9. Inline Formatting\n\nSuperscript: X²  \nSubscript: H₂O  \nEmoji: 🎉 🚀 🌍  \nHTML inside markdown: <mark>highlighted text</mark>  \n\n---\n\n## 10. Footnotes\n\nHere’s a statement with a footnote.[^1]  \n\n[^1]: This is the footnote explanation.  \n\n---\n\n## 11. Definition Lists\n\nTerm 1  \n: Definition of term 1  \n\nTerm 2  \n: Definition of term 2 with *emphasis*  \n\n---\n\n## 12. Escaping Characters\n\n\\*Not italic\\* but literal asterisks  \nUse a backslash for: \\# \\* \\[ \\] \\( \\)  \n\n---\n\n## 13. Code Blocks\n\n```csharp\n\nConsole.WriteLine(\"test\");\n\n```\n\n---\n\nThat’s the **full tour** of markdown features."
                  }
                ]
              }
            ]
            """);
    }

    // Avoid zero seconds span.
    await Task.Delay(100);

    activity?.Stop();

    return "Created GenAI trace";
});

app.MapGet("/genai-trace-display-error", async () =>
{
    var source = new ActivitySource("Services.Api", "1.0.0");

    var activity = source.StartActivity("chat gpt", ActivityKind.Client);
    if (activity != null)
    {
        activity.SetTag("gen_ai.system", "gpt");
        activity.SetTag("gen_ai.input.messages", "invalid");
    }

    // Avoid zero seconds span.
    await Task.Delay(100);

    activity?.Stop();

    return "Created GenAI trace";
});

async Task SimulateWorkAsync(ActivitySource source, int index, int millisecondsDelay = 2)
{
    using var activity = source.StartActivity($"WorkIteration{index + 1}");
    // Simulate some work in each iteration.
    await Task.Delay(millisecondsDelay);
}

app.MapGet("/big-nested-trace", async (HttpContext context) =>
{
    var source = new ActivitySource("Services.Api", "1.0.0");

    // Start activity as before.
    using var activity = source.StartActivity("HereActivity");

    // Prepare response for simple streaming text.
    context.Response.Headers["Content-Type"] = "text/plain; charset=utf-8";
    context.Response.Headers["Cache-Control"] = "no-cache";

    // Try to disable buffering if the server/proxy supports it.
    context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

    for (var i = 0; i < 10000; i++)
    {
        await SimulateWorkAsync(source, i);

        // Every 100 iterations, write a progress chunk and flush so the client receives it incrementally.
        if ((i + 1) % 100 == 0)
        {
            var msg = $"Progress: completed {i + 1} iterations\n";
            await context.Response.WriteAsync(msg);
            await context.Response.Body.FlushAsync();
        }
    }

    // Final message
    await context.Response.WriteAsync("Done\n");
    await context.Response.Body.FlushAsync();
});

app.Run();

public record WeatherForecast(DateOnly Date, int TemperatureC, string Summary);
