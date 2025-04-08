// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spectre.Console;
using RootCommand = Aspire.Cli.Commands.RootCommand;

namespace Aspire.Cli;

public class Program
{
    private static readonly ActivitySource s_activitySource = new ActivitySource(nameof(Program));

    private static IHost BuildApplication(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();

        // Always configure OpenTelemetry.
        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            });

        var otelBuilder = builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing => {
                tracing.AddSource(
                    nameof(NuGetPackageCache),
                    nameof(AppHostBackchannel),
                    nameof(DotNetCliRunner),
                    nameof(Program),
                    nameof(NewCommand),
                    nameof(RunCommand),
                    nameof(AddCommand),
                    nameof(PublishCommand)
                    );

                tracing.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("aspire-cli"));
            });

        // NOTE: Actual arg validation will take place elsewhere, this is just
        //       a quick check to make sure that DI is configured correctly.
        if (args.IndexOf("--cli-otlp-endpoint") is var otelArgIndex && otelArgIndex != -1)
        {
            // If we have an --cli-otlp-endpoint switch then we need to check that we
            // have at least enough args to have a base URL.
            if (args.Length == otelArgIndex + 2)
            {
                var baseUrl = args[otelArgIndex + 1];

                // If we have enough arguments we need to make sure its a valid URL.
                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps))
                {
                    otelBuilder.UseOtlpExporter(OpenTelemetry.Exporter.OtlpExportProtocol.Grpc, baseUri);
                }
            }
        }

        var debugMode = args?.Any(a => a == "--debug" || a == "-d") ?? false;

        if (debugMode)
        {
            builder.Logging.AddFilter("Aspire.Cli", LogLevel.Debug);
            builder.Logging.AddConsole();
        }

        // Shared services.
        builder.Services.AddTransient<DotNetCliRunner>();
        builder.Services.AddTransient<AppHostBackchannel>();
        builder.Services.AddSingleton<CliRpcTarget>();
        builder.Services.AddTransient<INuGetPackageCache, NuGetPackageCache>();

        // Commands.
        builder.Services.AddTransient<NewCommand>();
        builder.Services.AddTransient<RunCommand>();
        builder.Services.AddTransient<AddCommand>();
        builder.Services.AddTransient<PublishCommand>();
        builder.Services.AddTransient<RootCommand>();

        var app = builder.Build();
        return app;
    }

    public static async Task<int> Main(string[] args)
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        using var app = BuildApplication(args);

        await app.StartAsync().ConfigureAwait(false);

        var rootCommand = app.Services.GetRequiredService<RootCommand>();
        var config = new CommandLineConfiguration(rootCommand);
        config.EnableDefaultExceptionHandler = true;
        
        using var activity = s_activitySource.StartActivity();
        var exitCode = await config.InvokeAsync(args);

        await app.StopAsync().ConfigureAwait(false);

        return exitCode;
    }
}
