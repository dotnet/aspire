// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Agents.VsCode;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.Git;
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

        // Check if running MCP start command - all logs should go to stderr to keep stdout clean for MCP protocol
        var isMcpStartCommand = args?.Length >= 2 && args[0] == "mcp" && args[1] == "start";

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

        if (debugMode && !isMcpStartCommand)
        {
            builder.Logging.AddFilter("Aspire.Cli", LogLevel.Debug);
            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning); // Reduce noise from hosting lifecycle
            // Use custom Spectre Console logger for clean debug output instead of built-in console logger
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SpectreConsoleLoggerProvider>());
        }

        // For MCP start command, configure console logger to route all logs to stderr
        // This keeps stdout clean for MCP protocol JSON-RPC messages
        if (isMcpStartCommand)
        {
            if (debugMode)
            {
                builder.Logging.AddFilter("Aspire.Cli", LogLevel.Debug);
                builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning); // Reduce noise from hosting lifecycle                
            }

            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
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
        builder.Services.AddSingleton<AuxiliaryBackchannelMonitor>();
        builder.Services.AddSingleton<IAuxiliaryBackchannelMonitor>(sp => sp.GetRequiredService<AuxiliaryBackchannelMonitor>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<AuxiliaryBackchannelMonitor>());
        builder.Services.AddSingleton<ICliUpdateNotifier, CliUpdateNotifier>();
        builder.Services.AddSingleton<IPackagingService, PackagingService>();
        builder.Services.AddSingleton<ICliDownloader, CliDownloader>();
        builder.Services.AddMemoryCache();

        // Git repository operations.
        builder.Services.AddSingleton<IGitRepository, GitRepository>();

        // OpenCode CLI operations.
        builder.Services.AddSingleton<IOpenCodeCliRunner, OpenCodeCliRunner>();

        // Claude Code CLI operations.
        builder.Services.AddSingleton<IClaudeCodeCliRunner, ClaudeCodeCliRunner>();

        // VS Code CLI operations.
        builder.Services.AddSingleton<IVsCodeCliRunner, VsCodeCliRunner>();
        builder.Services.AddSingleton<ICopilotCliRunner, CopilotCliRunner>();

        // Agent environment detection.
        builder.Services.AddSingleton<IAgentEnvironmentDetector, AgentEnvironmentDetector>();
        builder.Services.AddSingleton<IAgentFingerprintService, AgentFingerprintService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEnvironmentScanner, VsCodeAgentEnvironmentScanner>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEnvironmentScanner, OpenCodeAgentEnvironmentScanner>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEnvironmentScanner, ClaudeCodeAgentEnvironmentScanner>());

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
        builder.Services.AddTransient<DoCommand>();
        builder.Services.AddTransient<ExecCommand>();
        builder.Services.AddTransient<McpCommand>();
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

    private static DirectoryInfo GetSdksDirectory()
    {
        var homeDirectory = GetUsersAspirePath();
        var sdksPath = Path.Combine(homeDirectory, "sdks");
        return new DirectoryInfo(sdksPath);
    }

    private static CliExecutionContext BuildCliExecutionContext(bool debugMode)
    {
        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        var hivesDirectory = GetHivesDirectory();
        var cacheDirectory = GetCacheDirectory();
        var sdksDirectory = GetSdksDirectory();
        return new CliExecutionContext(workingDirectory, hivesDirectory, cacheDirectory, sdksDirectory, debugMode);
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
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var isPlayground = CliHostEnvironment.IsPlaygroundMode(configuration);

        var settings = new AnsiConsoleSettings()
        {
            Ansi = isPlayground ? AnsiSupport.Yes : AnsiSupport.Detect,
            Interactive = isPlayground ? InteractionSupport.Yes : InteractionSupport.Detect,
            ColorSystem = isPlayground ? ColorSystemSupport.Standard : ColorSystemSupport.Detect,
        };

        if (isPlayground)
        {
            // Enrichers interfere with interactive playground experience so
            // this suppresses the default enrichers so that the CLI experience
            // is more like what we would get in an interactive experience.
            settings.Enrichment.UseDefaultEnrichers = false;
            settings.Enrichment.Enrichers = new()
            {
                new AspirePlaygroundEnricher()
            };
        }

        var ansiConsole = AnsiConsole.Create(settings);
        return ansiConsole;
    }

    public static async Task<int> Main(string[] args)
    {
        // Setup handling of CTRL-C as early as possible so that if
        // we get a CTRL-C anywhere that is not handled by Spectre Console
        // already that we know to trigger cancellation.
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            cts.Cancel();
            eventArgs.Cancel = true;
        };

        Console.OutputEncoding = Encoding.UTF8;

        using var app = await BuildApplicationAsync(args);

        await app.StartAsync().ConfigureAwait(false);

        var rootCommand = app.Services.GetRequiredService<RootCommand>();
        var invokeConfig = new InvocationConfiguration()
        {
            EnableDefaultExceptionHandler = true
        };

        var telemetry = app.Services.GetRequiredService<AspireCliTelemetry>();
        using var activity = telemetry.ActivitySource.StartActivity();
        var exitCode = await rootCommand.Parse(args).InvokeAsync(invokeConfig, cts.Token);

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

internal class AspirePlaygroundEnricher : IProfileEnricher
{
    public string Name => "Aspire Playground";

    public bool Enabled(IDictionary<string, string> environmentVariables)
    {
        if (!environmentVariables.TryGetValue("ASPIRE_PLAYGROUND", out var value))
        {
            return false;
        }

        if (!bool.TryParse(value, out var isEnabled))
        {
            return false;
        }

        return isEnabled;
    }

    public void Enrich(Profile profile)
    {
        profile.Capabilities.Interactive = true;
    }
}
