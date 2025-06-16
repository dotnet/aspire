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
using Aspire.Cli.NuGet;
using Aspire.Cli.Templating;
using Aspire.Cli.Configuration;
using Aspire.Cli.Extension;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Aspire.Cli.Utils;
using Aspire.Hosting;

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

    private static string GetGlobalSettingsPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalSettingsPath = Path.Combine(homeDirectory, ".aspire", "settings.json");
        return globalSettingsPath;
    }

    private static IHost BuildApplication(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        // Set up settings with appropriate paths.
        var globalSettingsFilePath = GetGlobalSettingsPath();
        var globalSettingsFile = new FileInfo(globalSettingsFilePath);
        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        ConfigurationHelper.RegisterSettingsFiles(builder.Configuration, workingDirectory, globalSettingsFile);

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

        if (builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is { })
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
        AddInteractionServices(builder);
        builder.Services.AddSingleton(BuildProjectLocator);
        builder.Services.AddSingleton<INewCommandPrompter, NewCommandPrompter>();
        builder.Services.AddSingleton<IAddCommandPrompter, AddCommandPrompter>();
        builder.Services.AddSingleton<IPublishCommandPrompter, PublishCommandPrompter>();
        builder.Services.AddSingleton<ICertificateService, CertificateService>();
        builder.Services.AddSingleton(BuildConfigurationService);
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

    private static IConfigurationService BuildConfigurationService(IServiceProvider serviceProvider)
    {
        var globalSettingsFile = new FileInfo(GetGlobalSettingsPath());
        return new ConfigurationService(new DirectoryInfo(Environment.CurrentDirectory), globalSettingsFile);
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
        var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        return new ProjectLocator(logger, runner, new DirectoryInfo(Environment.CurrentDirectory), interactionService, configurationService);
    }

    private static void AddInteractionServices(HostApplicationBuilder builder)
    {
        var extensionEndpoint = Environment.GetEnvironmentVariable(KnownConfigNames.ExtensionEndpoint);

        if (extensionEndpoint is not null)
        {
            builder.Services.AddSingleton<IExtensionBackchannel, ExtensionBackchannel>();
            builder.Services.AddSingleton<ExtensionBackchannelConnector>();
            builder.Services.AddHostedService<ExtensionBackchannelConnector>(provider => provider.GetRequiredService<ExtensionBackchannelConnector>());

            var extensionPromptEnabled = Environment.GetEnvironmentVariable(KnownConfigNames.ExtensionPromptEnabled) is "true";
            builder.Services.AddSingleton<IInteractionService>(provider =>
            {
                var ansiConsole = provider.GetRequiredService<IAnsiConsole>();
                var consoleInteractionService = new ConsoleInteractionService(ansiConsole);
                return new ExtensionInteractionService(consoleInteractionService,
                    provider.GetRequiredService<ExtensionBackchannelConnector>(),
                    extensionPromptEnabled);
            });
        }
        else
        {
            builder.Services.AddSingleton<IInteractionService, ConsoleInteractionService>();
        }
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
