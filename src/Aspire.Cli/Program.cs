// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Aspire.Cli.Utils;
using Aspire.Cli.Caching;
using Aspire.Hosting;
using Aspire.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using RootCommand = Aspire.Cli.Commands.RootCommand;
using Aspire.Cli.DotNet;
using Aspire.Cli.Packaging;

#if DEBUG
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
#endif

namespace Aspire.Cli;

public class Program
{
    private static string GetUsersAspirePath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var aspirePath = Path.Combine(homeDirectory, ".aspire");
        return aspirePath;
    }

    private static string GetGlobalSettingsPath()
    {
        var usersAspirePath = GetUsersAspirePath();
        var globalSettingsPath = Path.Combine(usersAspirePath, "globalsettings.json");
        return globalSettingsPath;
    }

    private static async Task<IHost> BuildApplicationAsync(string[] args)
    {
        // Check for --non-interactive flag early
        var nonInteractive = args?.Any(a => a == "--non-interactive") ?? false;

        var settings = new HostApplicationBuilderSettings
        {
            Configuration = new ConfigurationManager()
        };
        settings.Configuration.AddEnvironmentVariables();

        var builder = Host.CreateEmptyApplicationBuilder(settings);

        // Set up settings with appropriate paths.
        var globalSettingsFilePath = GetGlobalSettingsPath();
        var globalSettingsFile = new FileInfo(globalSettingsFilePath);
        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        ConfigurationHelper.RegisterSettingsFiles(builder.Configuration, workingDirectory, globalSettingsFile);

        await TrySetLocaleOverrideAsync(LocaleHelpers.GetLocaleOverride(builder.Configuration));

        // Always configure OpenTelemetry.
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

#if DEBUG
        var otelBuilder = builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
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
            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning); // Reduce noise from hosting lifecycle
            // Use custom Spectre Console logger for clean debug output instead of built-in console logger
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SpectreConsoleLoggerProvider>());
        }

        // Shared services.
        builder.Services.AddSingleton(_ => BuildCliExecutionContext(debugMode));
        builder.Services.AddSingleton(BuildAnsiConsole);
        builder.Services.AddSingleton<ICliHostEnvironment>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new CliHostEnvironment(configuration, nonInteractive);
        });
        AddInteractionServices(builder);
        builder.Services.AddSingleton<IProjectLocator, ProjectLocator>();
        builder.Services.AddSingleton<ISolutionLocator, SolutionLocator>();
        builder.Services.AddSingleton<FallbackProjectParser>();
        builder.Services.AddSingleton<IProjectUpdater, ProjectUpdater>();
        builder.Services.AddSingleton<INewCommandPrompter, NewCommandPrompter>();
        builder.Services.AddSingleton<IAddCommandPrompter, AddCommandPrompter>();
        builder.Services.AddSingleton<IPublishCommandPrompter, PublishCommandPrompter>();
        builder.Services.AddSingleton<ICertificateService, CertificateService>();
        builder.Services.AddSingleton(BuildConfigurationService);
        builder.Services.AddSingleton<IFeatures, Features>();
        builder.Services.AddSingleton<AspireCliTelemetry>();
        builder.Services.AddTransient<IDotNetCliRunner, DotNetCliRunner>();
    builder.Services.AddSingleton<IDiskCache, DiskCache>();
        builder.Services.AddSingleton<IDotNetSdkInstaller, DotNetSdkInstaller>();
        builder.Services.AddTransient<IAppHostBackchannel, AppHostBackchannel>();
        builder.Services.AddSingleton<INuGetPackageCache, NuGetPackageCache>();
        builder.Services.AddSingleton<NuGetPackagePrefetcher>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<NuGetPackagePrefetcher>());
        builder.Services.AddSingleton<ICliUpdateNotifier, CliUpdateNotifier>();
        builder.Services.AddSingleton<IPackagingService, PackagingService>();
        builder.Services.AddMemoryCache();

        // Template factories.
        builder.Services.AddSingleton<ITemplateProvider, TemplateProvider>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITemplateFactory, DotNetTemplateFactory>());

        // Commands.
        builder.Services.AddTransient<NewCommand>();
        builder.Services.AddTransient<InitCommand>();
        builder.Services.AddTransient<RunCommand>();
        builder.Services.AddTransient<AddCommand>();
        builder.Services.AddTransient<PublishCommand>();
        builder.Services.AddTransient<ConfigCommand>();
        builder.Services.AddTransient<CacheCommand>();
        builder.Services.AddTransient<UpdateCommand>();
        builder.Services.AddTransient<DeployCommand>();
        builder.Services.AddTransient<ExecCommand>();
        builder.Services.AddTransient<RootCommand>();
        builder.Services.AddTransient<ExtensionInternalCommand>();

        var app = builder.Build();
        return app;
    }

    private static DirectoryInfo GetHivesDirectory()
    {
        var homeDirectory = GetUsersAspirePath();
        var hivesDirectory = Path.Combine(homeDirectory, "hives");
        return new DirectoryInfo(hivesDirectory);
    }

    private static CliExecutionContext BuildCliExecutionContext(bool debugMode)
    {
        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        var hivesDirectory = GetHivesDirectory();
        var cacheDirectory = GetCacheDirectory();
        return new CliExecutionContext(workingDirectory, hivesDirectory, cacheDirectory, debugMode);
    }

    private static DirectoryInfo GetCacheDirectory()
    {
        var homeDirectory = GetUsersAspirePath();
        var cacheDirectoryPath = Path.Combine(homeDirectory, "cache");
        return new DirectoryInfo(cacheDirectoryPath);
    }

    private static async Task TrySetLocaleOverrideAsync(string? localeOverride)
    {
        if (localeOverride is not null)
        {
            var result = LocaleHelpers.TrySetLocaleOverride(localeOverride);

            string errorMessage;
            switch (result)
            {
                case SetLocaleResult.Success:
                    return;
                case SetLocaleResult.InvalidLocale:
                    errorMessage = string.Format(CultureInfo.CurrentCulture, ErrorStrings.UnsupportedLocaleProvided, localeOverride, string.Join(", ", LocaleHelpers.SupportedLocales));
                    break;
                case SetLocaleResult.UnsupportedLocale:
                    errorMessage = string.Format(CultureInfo.CurrentCulture, ErrorStrings.InvalidLocaleProvided, localeOverride);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected result: {result}");
            }

            await Console.Error.WriteLineAsync(errorMessage);
        }
    }

    private static IConfigurationService BuildConfigurationService(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var executionContext = serviceProvider.GetRequiredService<CliExecutionContext>();
        var globalSettingsFile = new FileInfo(GetGlobalSettingsPath());
        return new ConfigurationService(configuration, executionContext, globalSettingsFile);
    }

    private static IAnsiConsole BuildAnsiConsole(IServiceProvider serviceProvider)
    {
        var settings = new AnsiConsoleSettings()
        {
            Ansi = AnsiSupport.Detect,
            Interactive = InteractionSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect
        };

        var ansiConsole = AnsiConsole.Create(settings);
        return ansiConsole;
    }

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        using var app = await BuildApplicationAsync(args);

        await app.StartAsync().ConfigureAwait(false);

        var rootCommand = app.Services.GetRequiredService<RootCommand>();
        var invokeConfig = new InvocationConfiguration()
        {
            EnableDefaultExceptionHandler = true,
            // HACK: Workaround until we get 10.0 RC2: https://github.com/dotnet/command-line-api/pull/2674/files
            ProcessTerminationTimeout = TimeSpan.FromSeconds(2)
        };

        var telemetry = app.Services.GetRequiredService<AspireCliTelemetry>();
        using var activity = telemetry.ActivitySource.StartActivity();
        var exitCode = await rootCommand.Parse(args).InvokeAsync(invokeConfig);

        await app.StopAsync().ConfigureAwait(false);

        return exitCode;
    }

    private static void AddInteractionServices(HostApplicationBuilder builder)
    {
        var extensionEndpoint = builder.Configuration[KnownConfigNames.ExtensionEndpoint];

        if (extensionEndpoint is not null)
        {
            builder.Services.AddSingleton<IExtensionRpcTarget, ExtensionRpcTarget>();
            builder.Services.AddSingleton<IExtensionBackchannel, ExtensionBackchannel>();

            var extensionPromptEnabled = builder.Configuration[KnownConfigNames.ExtensionPromptEnabled] is "true";
            builder.Services.AddSingleton<IInteractionService>(provider =>
            {
                var ansiConsole = provider.GetRequiredService<IAnsiConsole>();
                ansiConsole.Profile.Width = 256; // VS code terminal will handle wrapping so set a large width here.
                var executionContext = provider.GetRequiredService<CliExecutionContext>();
                var hostEnvironment = provider.GetRequiredService<ICliHostEnvironment>();
                var consoleInteractionService = new ConsoleInteractionService(ansiConsole, executionContext, hostEnvironment);
                return new ExtensionInteractionService(consoleInteractionService,
                    provider.GetRequiredService<IExtensionBackchannel>(),
                    extensionPromptEnabled);
            });

            // If the CLI is being launched from the aspire extension, we don't want to use the console logger that's used when including --debug.
            // Instead, we will log to the extension backchannel.
            builder.Logging.AddFilter("Aspire.Cli", LogLevel.Trace);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ExtensionLoggerProvider>());
        }
        else
        {
            builder.Services.AddSingleton<IInteractionService>(provider =>
            {
                var ansiConsole = provider.GetRequiredService<IAnsiConsole>();
                var executionContext = provider.GetRequiredService<CliExecutionContext>();
                var hostEnvironment = provider.GetRequiredService<ICliHostEnvironment>();
                return new ConsoleInteractionService(ansiConsole, executionContext, hostEnvironment);
            });
        }
    }
}
