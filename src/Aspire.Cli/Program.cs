// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Agents.CopilotCli;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Agents.VsCode;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Caching;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Commands.Sdk;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Git;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Scaffolding;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Aspire.Cli.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;
using Aspire.Hosting;
using Aspire.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using RootCommand = Aspire.Cli.Commands.RootCommand;

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

    internal static async Task<IHost> BuildApplicationAsync(string[] args, Dictionary<string, string?>? configurationValues = null)
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

        if (configurationValues is not null)
        {
            settings.Configuration.AddInMemoryCollection(configurationValues);
        }

        var builder = Host.CreateEmptyApplicationBuilder(settings);

        // Set up settings with appropriate paths.
        var globalSettingsFilePath = GetGlobalSettingsPath();
        var globalSettingsFile = new FileInfo(globalSettingsFilePath);
        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        ConfigurationHelper.RegisterSettingsFiles(builder.Configuration, workingDirectory, globalSettingsFile);

        await TrySetLocaleOverrideAsync(LocaleHelpers.GetLocaleOverride(builder.Configuration));

#if !DEBUG
        // In release builds, limit shutdown wait time for telemetry flush to 200ms
        // to ensure the CLI exits quickly even if waiting on shutdown tasks.
        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromMilliseconds(200);
        });
#endif

        // Always configure OpenTelemetry.
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // Configure OpenTelemetry tracing. TelemetryManager reads configuration and creates
        // separate TracerProvider instances:
        // - Azure Monitor provider with filtering (only exports activities with EXTERNAL_TELEMETRY=true)
        // - Diagnostic provider for OTLP/console exporters (exports all activities, DEBUG only)
        builder.Services.AddSingleton(new TelemetryManager(builder.Configuration));

        var debugMode = args?.Any(a => a == "--debug" || a == "-d") ?? false;
        var extensionEndpoint = builder.Configuration[KnownConfigNames.ExtensionEndpoint];

        if (debugMode && !isMcpStartCommand && extensionEndpoint is null)
        {
            builder.Logging.AddFilter("Aspire.Cli", LogLevel.Debug);
            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning); // Reduce noise from hosting lifecycle
            // Use custom Spectre Console logger for clean debug output to stderr
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(new SpectreConsoleLoggerProvider(Console.Error)));
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
        builder.Services.AddSingleton(s => BuildAnsiConsole(s, Console.Out));
        builder.Services.AddSingleton<ICliHostEnvironment>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new CliHostEnvironment(configuration, nonInteractive);
        });
        builder.Services.AddSingleton(TimeProvider.System);
        AddInteractionServices(builder);
        builder.Services.AddSingleton<IProjectLocator, ProjectLocator>();
        builder.Services.AddSingleton<ISolutionLocator, SolutionLocator>();
        builder.Services.AddSingleton<ILanguageService, LanguageService>();
        builder.Services.AddSingleton<IScaffoldingService, ScaffoldingService>();
        builder.Services.AddSingleton<FallbackProjectParser>();
        builder.Services.AddSingleton<IProjectUpdater, ProjectUpdater>();
        builder.Services.AddSingleton<INewCommandPrompter, NewCommandPrompter>();
        builder.Services.AddSingleton<IAddCommandPrompter, AddCommandPrompter>();
        builder.Services.AddSingleton<IPublishCommandPrompter, PublishCommandPrompter>();
        builder.Services.AddSingleton<ICertificateService, CertificateService>();
        builder.Services.AddSingleton(BuildConfigurationService);
        builder.Services.AddSingleton<IFeatures, Features>();
        builder.Services.AddTelemetryServices();
        builder.Services.AddTransient<IDotNetCliExecutionFactory, DotNetCliExecutionFactory>();
        builder.Services.AddTransient<IDotNetCliRunner, DotNetCliRunner>();
        builder.Services.AddSingleton<IDiskCache, DiskCache>();
        builder.Services.AddSingleton<IDotNetSdkInstaller, DotNetSdkInstaller>();
        builder.Services.AddTransient<IAppHostCliBackchannel, AppHostCliBackchannel>();
        builder.Services.AddSingleton<INuGetPackageCache, NuGetPackageCache>();
        builder.Services.AddSingleton<NuGetPackagePrefetcher>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<NuGetPackagePrefetcher>());
        builder.Services.AddSingleton<AuxiliaryBackchannelMonitor>();
        builder.Services.AddSingleton<IAuxiliaryBackchannelMonitor>(sp => sp.GetRequiredService<AuxiliaryBackchannelMonitor>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<AuxiliaryBackchannelMonitor>());
        builder.Services.AddSingleton<ICliUpdateNotifier, CliUpdateNotifier>();
        builder.Services.AddSingleton<IPackagingService, PackagingService>();
        builder.Services.AddSingleton<IAppHostServerProjectFactory, AppHostServerProjectFactory>();
        builder.Services.AddSingleton<ICliDownloader, CliDownloader>();
        builder.Services.AddSingleton<IFirstTimeUseNoticeSentinel>(_ => new FirstTimeUseNoticeSentinel(GetUsersAspirePath()));
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
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEnvironmentScanner, VsCodeAgentEnvironmentScanner>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEnvironmentScanner, CopilotCliAgentEnvironmentScanner>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEnvironmentScanner, OpenCodeAgentEnvironmentScanner>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentEnvironmentScanner, ClaudeCodeAgentEnvironmentScanner>());

        // Template factories.
        builder.Services.AddSingleton<ITemplateProvider, TemplateProvider>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITemplateFactory, DotNetTemplateFactory>());

        // Language discovery for polyglot support.
        builder.Services.AddSingleton<ILanguageDiscovery, DefaultLanguageDiscovery>();

        // AppHost server session factory for RPC communication.
        builder.Services.AddSingleton<IAppHostServerSessionFactory, AppHostServerSessionFactory>();

        // AppHost project handlers.
        builder.Services.AddSingleton<DotNetAppHostProject>();
        builder.Services.AddSingleton<Func<LanguageInfo, GuestAppHostProject>>(sp =>
        {
            return language => ActivatorUtilities.CreateInstance<GuestAppHostProject>(sp, language);
        });
        builder.Services.AddSingleton<IAppHostProjectFactory, AppHostProjectFactory>();

        // Environment checking services.
        builder.Services.AddSingleton<IEnvironmentCheck, WslEnvironmentCheck>();
        builder.Services.AddSingleton<IEnvironmentCheck, DotNetSdkCheck>();
        builder.Services.AddSingleton<IEnvironmentCheck, DeprecatedWorkloadCheck>();
        builder.Services.AddSingleton<IEnvironmentCheck, DevCertsCheck>();
        builder.Services.AddSingleton<IEnvironmentCheck, ContainerRuntimeCheck>();
        builder.Services.AddSingleton<IEnvironmentChecker, EnvironmentChecker>();

        // Commands.
        builder.Services.AddTransient<NewCommand>();
        builder.Services.AddTransient<InitCommand>();
        builder.Services.AddTransient<RunCommand>();
        builder.Services.AddTransient<StopCommand>();
        builder.Services.AddTransient<PsCommand>();
        builder.Services.AddTransient<ResourcesCommand>();
        builder.Services.AddTransient<LogsCommand>();
        builder.Services.AddTransient<AddCommand>();
        builder.Services.AddTransient<PublishCommand>();
        builder.Services.AddTransient<ConfigCommand>();
        builder.Services.AddTransient<CacheCommand>();
        builder.Services.AddTransient<DoctorCommand>();
        builder.Services.AddTransient<UpdateCommand>();
        builder.Services.AddTransient<DeployCommand>();
        builder.Services.AddTransient<DoCommand>();
        builder.Services.AddTransient<ExecCommand>();
        builder.Services.AddTransient<McpCommand>();
        builder.Services.AddTransient<SdkCommand>();
        builder.Services.AddTransient<SdkGenerateCommand>();
        builder.Services.AddTransient<SdkDumpCommand>();
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

    private static void DisplayFirstTimeUseNoticeIfNeeded(IServiceProvider serviceProvider, bool noLogo)
    {
        var sentinel = serviceProvider.GetRequiredService<IFirstTimeUseNoticeSentinel>();

        if (sentinel.Exists())
        {
            return;
        }

        if (!noLogo)
        {
            // Write to stderr to avoid interfering with tools that parse stdout
            var stderrConsole = BuildAnsiConsole(serviceProvider, Console.Error);

            // Display welcome. Matches ConsoleInteractionService.DisplayMessage to display a message with emoji consistently.
            stderrConsole.Markup(":waving_hand:");
            stderrConsole.Write("\u001b[4G");
            stderrConsole.MarkupLine(RootCommandStrings.FirstTimeUseWelcome);

            stderrConsole.WriteLine();
            stderrConsole.WriteLine(RootCommandStrings.FirstTimeUseTelemetryNotice);
            stderrConsole.WriteLine();
        }

        sentinel.CreateIfNotExists();
    }

    private static IAnsiConsole BuildAnsiConsole(IServiceProvider serviceProvider, TextWriter writer)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var hostEnvironment = serviceProvider.GetRequiredService<ICliHostEnvironment>();
        var isPlayground = CliHostEnvironment.IsPlaygroundMode(configuration);

        // Create custom output that handles width detection better in CI environments
        // and encapsulates ASPIRE_CONSOLE_WIDTH environment variable handling
        var output = new AspireAnsiConsoleOutput(writer, configuration);

        var settings = new AnsiConsoleSettings()
        {
            Ansi = isPlayground ? AnsiSupport.Yes : AnsiSupport.Detect,
            Interactive = isPlayground ? InteractionSupport.Yes : InteractionSupport.Detect,
            ColorSystem = isPlayground ? ColorSystemSupport.Standard : ColorSystemSupport.Detect,
            Out = output,
        };

        // Use SupportsAnsi from hostEnvironment which already checks ASPIRE_ANSI_PASS_THRU
        if (hostEnvironment.SupportsAnsi)
        {
            settings.Ansi = AnsiSupport.Yes;
            settings.ColorSystem = ColorSystemSupport.Standard;
        }

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

        // Display first run experience if this is the first time the CLI is run on this machine
        var noLogo = args.Any(a => a == "--nologo");
        DisplayFirstTimeUseNoticeIfNeeded(app.Services, noLogo);

        var rootCommand = app.Services.GetRequiredService<RootCommand>();
        var invokeConfig = new InvocationConfiguration()
        {
            // Disable default exception handler so we can log exceptions to telemetry.
            EnableDefaultExceptionHandler = false
        };

        var telemetry = app.Services.GetRequiredService<AspireCliTelemetry>();
        var telemetryManager = app.Services.GetRequiredService<TelemetryManager>();
        using var mainActivity = telemetry.StartReportedActivity(name: TelemetryConstants.Activities.Main, kind: ActivityKind.Internal);

        if (mainActivity != null)
        {
            var currentProcess = Process.GetCurrentProcess();
            mainActivity.SetStartTime(currentProcess.StartTime);
            mainActivity.AddTag(TelemetryConstants.Tags.ProcessPid, currentProcess.Id);
            mainActivity.AddTag(TelemetryConstants.Tags.ProcessExecutableName, "aspire");
        }

        try
        {
            var parseResult = rootCommand.Parse(args);

            mainActivity?.SetTag(TelemetryConstants.Tags.CommandName, GetCommandName(parseResult));

            var exitCode = await parseResult.InvokeAsync(invokeConfig, cts.Token);

            mainActivity?.SetTag(TelemetryConstants.Tags.ProcessExitCode, exitCode);
            mainActivity?.Stop();

            return exitCode;
        }
        catch (Exception ex)
        {
            const int unknownErrorExitCode = 1;
            // Catch block is used instead of System.Commandline's default handler behavior.
            // Allows logging of exceptions to telemetry.

            // Don't log or display cancellation exceptions.
            if (!(ex is OperationCanceledException && cts.IsCancellationRequested))
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An unexpected error occurred.");

                telemetry.RecordError("An unexpected error occurred.", ex);

                var interactionService = app.Services.GetRequiredService<IInteractionService>();
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message));
            }

            mainActivity?.SetTag(TelemetryConstants.Tags.ProcessExitCode, unknownErrorExitCode);
            mainActivity?.Stop();

            return unknownErrorExitCode;
        }
        finally
        {
            // Shutting down telemetry manager to flush any remaining telemetry and will take time.
            // Start shutdown of telemetry manager immediately and run concurrently with app shutdown.
            var shutdownTelemetryTask = telemetryManager.ShutdownAsync();

            await app.StopAsync().ConfigureAwait(false);
            await shutdownTelemetryTask;
        }
    }

    private static string GetCommandName(ParseResult r)
    {
        // Walk the parent command tree to find the top-level command name and get the full command name for this parseresult.
        var parentNames = new List<string> { r.CommandResult.Command.Name };
        var current = r.CommandResult.Parent;
        while (current is CommandResult parentCommandResult)
        {
            parentNames.Add(parentCommandResult.Command.Name);
            current = parentCommandResult.Parent;
        }
        parentNames.Reverse();
        return string.Join(' ', parentNames);
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
