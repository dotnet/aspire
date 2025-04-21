// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Microsoft.Extensions.Configuration;


#if DEBUG
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
#endif

using RootCommand = Aspire.Cli.Commands.RootCommand;

namespace Aspire.Cli;

public class Program
{
    private static readonly ActivitySource s_activitySource = new ActivitySource(nameof(Program));

    private static void AddConfigurationFiles(HostApplicationBuilder builder)
    {
        
    }

    private static IHost BuildApplication(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();

        // Always configure OpenTelemetry.
        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            });

#if DEBUG
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

        if (builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is {})
        {
            // NOTE: If we always enable the OTEL exporter it dramatically
            //       impacts the CLI in terms of exiting quickly because it
            //       has to finish sending telemetry.
            otelBuilder.UseOtlpExporter();
        }
#endif

        var debugMode = args?.Any(a => a == "--debug" || a == "-d") ?? false;

        if (debugMode)
        {
            builder.Logging.AddFilter("Aspire.Cli", LogLevel.Debug);
            builder.Logging.AddConsole();
        }

        // Shared services.
        builder.Services.AddSingleton(BuildAnsiConsole);
        builder.Services.AddSingleton(BuildProjectLocator);
        builder.Services.AddSingleton<INewCommandPrompter, NewCommandPrompter>();
        builder.Services.AddSingleton<IAddCommandPrompter, AddCommandPrompter>();
        builder.Services.AddSingleton<IPublishCommandPrompter, PublishCommandPrompter>();
        builder.Services.AddSingleton<IInteractionService, InteractionService>();
        builder.Services.AddSingleton<ICertificateService, CertificateService>();
        builder.Services.AddTransient<IDotNetCliRunner, DotNetCliRunner>();
        builder.Services.AddTransient<IAppHostBackchannel, AppHostBackchannel>();
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

    private static IAnsiConsole BuildAnsiConsole(IServiceProvider serviceProvider)
    {
        AnsiConsoleSettings settings = new AnsiConsoleSettings()
        {
            Ansi = AnsiSupport.Detect,
            Interactive = InteractionSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect
        };
        var ansiConsole = AnsiConsole.Create(settings);
        return ansiConsole;
    }

    private static IProjectLocator BuildProjectLocator(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ProjectLocator>>();
        var runner = serviceProvider.GetRequiredService<IDotNetCliRunner>();
        return new ProjectLocator(logger, runner, Directory.GetCurrentDirectory());
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
