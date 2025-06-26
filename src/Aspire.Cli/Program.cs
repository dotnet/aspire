// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
using Aspire.Cli.Resources;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Aspire.Cli.Utils;
using Aspire.Cli.Telemetry;
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

    private static string GetGlobalSettingsPath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalSettingsPath = ConfigurationHelper.BuildPathToSettingsJsonFile(homeDirectory);
        return globalSettingsPath;
    }

    private static async Task<IHost> BuildApplicationAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        // Set up settings with appropriate paths.
        var globalSettingsFilePath = GetGlobalSettingsPath();
        var globalSettingsFile = new FileInfo(globalSettingsFilePath);
        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        ConfigurationHelper.RegisterSettingsFiles(builder.Configuration, workingDirectory, globalSettingsFile);

        await TrySetLocaleOverrideAsync(builder.Configuration);

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
                tracing.AddSource(AspireCliTelemetry.ActivitySourceName);

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
        builder.Services.AddSingleton<AspireCliTelemetry>();
        builder.Services.AddTransient<IDotNetCliRunner, DotNetCliRunner>();
        builder.Services.AddTransient<IAppHostBackchannel, AppHostBackchannel>();
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
        builder.Services.AddTransient<DeployCommand>();
        builder.Services.AddTransient<ExecCommand>();
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
        var telemetry = serviceProvider.GetRequiredService<AspireCliTelemetry>();
        return new ProjectLocator(logger, runner, new DirectoryInfo(Environment.CurrentDirectory), interactionService, configurationService, telemetry);
    }

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        using var app = await BuildApplicationAsync(args);

        await app.StartAsync().ConfigureAwait(false);

        var rootCommand = app.Services.GetRequiredService<RootCommand>();
        var config = new CommandLineConfiguration(rootCommand);
        config.EnableDefaultExceptionHandler = true;

        var telemetry = app.Services.GetRequiredService<AspireCliTelemetry>();
        using var activity = telemetry.ActivitySource.StartActivity();
        var exitCode = await config.InvokeAsync(args);

        await app.StopAsync().ConfigureAwait(false);

        return exitCode;
    }

    private static void AddInteractionServices(HostApplicationBuilder builder)
    {
        var extensionEndpoint = builder.Configuration[KnownConfigNames.ExtensionEndpoint];

        if (extensionEndpoint is not null)
        {
            builder.Services.AddSingleton<ExtensionRpcTarget>();
            builder.Services.AddSingleton<IExtensionBackchannel, ExtensionBackchannel>();

            var extensionPromptEnabled = builder.Configuration[KnownConfigNames.ExtensionPromptEnabled] is "true";
            builder.Services.AddSingleton<IInteractionService>(provider =>
            {
                var ansiConsole = provider.GetRequiredService<IAnsiConsole>();
                var consoleInteractionService = new ConsoleInteractionService(ansiConsole);
                return new ExtensionInteractionService(consoleInteractionService,
                    provider.GetRequiredService<IExtensionBackchannel>(),
                    extensionPromptEnabled);
            });
        }
        else
        {
            builder.Services.AddSingleton<IInteractionService, ConsoleInteractionService>();
        }
    }

    private static readonly string[] s_supportedLocales = ["en", "cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-CN", "zh-TW"];

    private static async Task TrySetLocaleOverrideAsync(ConfigurationManager configuration)
    {
        var localeOverride = configuration[KnownConfigNames.CliLocaleOverride]
                             // also support DOTNET_CLI_UI_LANGUAGE as it's a common dotnet environment variable
                             ?? configuration[KnownConfigNames.DotnetCliUiLanguage];
        if (localeOverride is not null)
        {
            if (!TrySetLocaleOverride(localeOverride, out var errorMessage))
            {
                await Console.Error.WriteLineAsync(errorMessage);
            }
        }

        return;

        static bool TrySetLocaleOverride(string localeOverride, [NotNullWhen(false)] out string? errorMessage)
        {
            try
            {
                var cultureInfo = new CultureInfo(localeOverride);
                if (s_supportedLocales.Contains(cultureInfo.Name) ||
                    s_supportedLocales.Contains(cultureInfo.TwoLetterISOLanguageName))
                {
                    CultureInfo.CurrentUICulture = cultureInfo;
                    CultureInfo.CurrentCulture = cultureInfo;
                    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                    errorMessage = null;
                    return true;
                }

                errorMessage = string.Format(CultureInfo.CurrentCulture, ErrorStrings.UnsupportedLocaleProvided, localeOverride, string.Join(", ", s_supportedLocales));
                return false;
            }
            catch (CultureNotFoundException)
            {
                errorMessage = string.Format(CultureInfo.CurrentCulture, ErrorStrings.InvalidLocaleProvided, localeOverride);
                return false;
            }
        }
    }
}
