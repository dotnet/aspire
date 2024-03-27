// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Stress.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(BigTraceCreator.ActivitySourceName));

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(ConsoleStresser.Stress);

app.MapGet("/", () => "Hello world");

app.MapGet("/big-trace", async () =>
{
    var bigTraceCreator = new BigTraceCreator();

    await bigTraceCreator.CreateBigTraceAsync();

    return "Big trace created";
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

app.Run();
