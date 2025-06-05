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
using Aspire.Cli.NuGet;
using Aspire.Cli.Templating;
using Aspire.Cli.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    /// <summary>
    /// Sets up the application configuration by loading settings from .aspire/settings.json files.
    /// Loads the nearest local settings file and global settings from $HOME/.aspire/settings.json,
    /// with global settings taking precedence over local settings.
    /// </summary>
    private static void SetupAppHostOptions(HostApplicationBuilder builder)
    {
        // Find the nearest local settings file
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        FileInfo? localSettingsFile = null;

        while (currentDirectory is not null)
        {
            var settingsFilePath = Path.Combine(currentDirectory.FullName, ".aspire", "settings.json");

            if (File.Exists(settingsFilePath))
            {
                localSettingsFile = new FileInfo(settingsFilePath);
                break;
            }

            currentDirectory = currentDirectory.Parent;
        }

        // Add local settings first (if found)
        if (localSettingsFile is not null)
        {
            builder.Configuration.AddJsonFile(localSettingsFile.FullName, optional: true);
        }

        // Then add global settings file (if it exists) - this will override local settings
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalSettingsPath = Path.Combine(homeDirectory, ".aspire", "settings.json");
        if (File.Exists(globalSettingsPath))
        {
            builder.Configuration.AddJsonFile(globalSettingsPath, optional: true);
        }
    }

    private static IHost BuildApplication(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        SetupAppHostOptions(builder);

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
        builder.Services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();
        builder.Services.AddTransient<IDotNetCliRunner, DotNetCliRunner>();
        builder.Services.AddTransient<IAppHostBackchannel, AppHostBackchannel>();
        builder.Services.AddSingleton<CliRpcTarget>();
        builder.Services.AddSingleton<INuGetPackageCache, NuGetPackageCache>();
        builder.Services.AddHostedService(BuildNuGetPackagePrefetcher);
        builder.Services.AddMemoryCache();

        // Template factories.
        builder.Services.AddSingleton<ITemplateProvider, TemplateProvider>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITemplateFactory, DotNetTemplateFactory>());

        // Commands.
        builder.Services.AddTransient<NewCommand>();
        builder.Services.AddTransient<RunCommand>();
        builder.Services.AddTransient<AddCommand>();
        builder.Services.AddTransient<PublishCommand>();
        builder.Services.AddTransient<ConfigCommand>();
        builder.Services.AddTransient<RootCommand>();

        var app = builder.Build();
        return app;
    }

    private static NuGetPackagePrefetcher BuildNuGetPackagePrefetcher(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<NuGetPackagePrefetcher>>();
        var nuGetPackageCache = serviceProvider.GetRequiredService<INuGetPackageCache>();
        var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        return new NuGetPackagePrefetcher(logger, nuGetPackageCache, currentDirectory);
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
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new ProjectLocator(logger, runner, new DirectoryInfo(Environment.CurrentDirectory), interactionService);
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
