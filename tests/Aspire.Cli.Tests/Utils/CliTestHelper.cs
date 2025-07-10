// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Projects;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Aspire.Cli.Configuration;
using Xunit;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Utils;

internal static class CliTestHelper
{
    public static IServiceCollection CreateServiceCollection(TemporaryWorkspace workspace, ITestOutputHelper outputHelper, Action<CliServiceCollectionTestOptions>? configure = null)
    {
        var options = new CliServiceCollectionTestOptions(outputHelper, workspace.WorkspaceRoot);
        configure?.Invoke(options);

        var services = new ServiceCollection();

        var configBuilder = new ConfigurationBuilder();

        var configurationValues = new Dictionary<string, string?>();

        // Populate feature flag configuration in in-memory collection.
        options.ConfigurationCallback += config => {
            foreach (var featureFlag in options.EnabledFeatures)
            {
                config[$"{KnownFeatures.FeaturePrefix}:{featureFlag}"] = "true";
            }

            foreach (var featureFlag in options.DisabledFeatures)
            {
                config[$"{KnownFeatures.FeaturePrefix}:{featureFlag}"] = "false";
            }
        };

        options.ConfigurationCallback(configurationValues);

        configBuilder.AddInMemoryCollection(configurationValues);

        var globalSettingsFilePath = Path.Combine(options.WorkingDirectory.FullName, ".aspire", "settings.global.json");
        var globalSettingsFile = new FileInfo(globalSettingsFilePath);
        ConfigurationHelper.RegisterSettingsFiles(configBuilder, options.WorkingDirectory, globalSettingsFile);

        var configuration = configBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging();

        services.AddMemoryCache();

        services.AddSingleton(options.AnsiConsoleFactory);
        services.AddSingleton(options.TelemetryFactory);
        services.AddSingleton(options.ProjectLocatorFactory);
        services.AddSingleton(options.InteractionServiceFactory);
        services.AddSingleton(options.CertificateServiceFactory);
        services.AddSingleton(options.NewCommandPrompterFactory);
        services.AddSingleton(options.AddCommandPrompterFactory);
        services.AddSingleton(options.PublishCommandPrompterFactory);
        services.AddTransient(options.DotNetCliRunnerFactory);
        services.AddTransient(options.NuGetPackageCacheFactory);
        services.AddSingleton(options.TemplateProviderFactory);
        services.AddSingleton(options.ConfigurationServiceFactory);
        services.AddSingleton(options.FeatureFlagsFactory);
        services.AddSingleton(options.CliUpdateNotifierFactory);
        services.AddTransient<RootCommand>();
        services.AddTransient<NewCommand>();
        services.AddTransient<RunCommand>();
        services.AddTransient<ExecCommand>();
        services.AddTransient<AddCommand>();
        services.AddTransient<DeployCommand>();
        services.AddTransient<PublishCommand>();
        services.AddTransient<ConfigCommand>();
        services.AddTransient(options.AppHostBackchannelFactory);

        return services;
    }
}

internal sealed class CliServiceCollectionTestOptions
{
    private readonly ITestOutputHelper _outputHelper;

    public CliServiceCollectionTestOptions(ITestOutputHelper outputHelper, DirectoryInfo workingDirectory)
    {
        _outputHelper = outputHelper;
        WorkingDirectory = workingDirectory;

        ProjectLocatorFactory = CreateDefaultProjectLocatorFactory;
        ConfigurationServiceFactory = CreateDefaultConfigurationServiceFactory;
    }

    public DirectoryInfo WorkingDirectory { get; set; }

    public Action<Dictionary<string, string?>> ConfigurationCallback { get; set; } = (Dictionary<string, string?> config) =>
    {
    };

    public string[] EnabledFeatures { get; set; } = Array.Empty<string>();
    public string[] DisabledFeatures { get; set; } = Array.Empty<string>();

    public Func<IServiceProvider, IAnsiConsole> AnsiConsoleFactory => (IServiceProvider serviceProvider) =>
    {
        AnsiConsoleSettings settings = new AnsiConsoleSettings()
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            ColorSystem = ColorSystemSupport.Standard,
            Out = new AnsiConsoleOutput(new TestOutputTextWriter(_outputHelper))
        };
        var ansiConsole = AnsiConsole.Create(settings);
        return ansiConsole;
    };

    public Func<IServiceProvider, INewCommandPrompter> NewCommandPrompterFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new NewCommandPrompter(interactionService);
    };

    public Func<IServiceProvider, ICliUpdateNotifier> CliUpdateNotifierFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<CliUpdateNotifier>();
        var nuGetPackageCache = serviceProvider.GetRequiredService<INuGetPackageCache>();
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new CliUpdateNotifier(logger, nuGetPackageCache, interactionService);
    };

    public Func<IServiceProvider, IAddCommandPrompter> AddCommandPrompterFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new AddCommandPrompter(interactionService);
    };

    public Func<IServiceProvider, IPublishCommandPrompter> PublishCommandPrompterFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new PublishCommandPrompter(interactionService);
    };

    public Func<IServiceProvider, IConfigurationService> ConfigurationServiceFactory { get; set; }

    public IConfigurationService CreateDefaultConfigurationServiceFactory(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return new ConfigurationService(configuration, WorkingDirectory, GetGlobalSettingsFile(WorkingDirectory));
    }

    private static FileInfo GetGlobalSettingsFile(DirectoryInfo workingDirectory)
    {
        var globalSettingsFilePath = Path.Combine(workingDirectory.FullName, ".aspire", "settings.global.json");
        return new FileInfo(globalSettingsFilePath);
    }

    public Func<IServiceProvider, IProjectLocator> ProjectLocatorFactory { get; set; }

    public IProjectLocator CreateDefaultProjectLocatorFactory(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ProjectLocator>>();
        var runner = serviceProvider.GetRequiredService<IDotNetCliRunner>();
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        var telemetry = serviceProvider.GetRequiredService<AspireCliTelemetry>();
        return new ProjectLocator(logger, runner, WorkingDirectory, interactionService, configurationService, telemetry);
    }

    public Func<IServiceProvider, AspireCliTelemetry> TelemetryFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        return new AspireCliTelemetry();
    };

    public Func<IServiceProvider, IInteractionService> InteractionServiceFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var ansiConsole = serviceProvider.GetRequiredService<IAnsiConsole>();
        return new ConsoleInteractionService(ansiConsole);
    };

    public Func<IServiceProvider, ICertificateService> CertificateServiceFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var interactiveService = serviceProvider.GetRequiredService<IInteractionService>();
        var telemetry = serviceProvider.GetRequiredService<AspireCliTelemetry>();
        return new CertificateService(interactiveService, telemetry);
    };

    public Func<IServiceProvider, IDotNetCliRunner> DotNetCliRunnerFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<DotNetCliRunner>>();
        var telemetry = serviceProvider.GetRequiredService<AspireCliTelemetry>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return new DotNetCliRunner(logger, serviceProvider, telemetry, configuration);
    };

    public Func<IServiceProvider, INuGetPackageCache> NuGetPackageCacheFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<NuGetPackageCache>>();
        var runner = serviceProvider.GetRequiredService<IDotNetCliRunner>();
        var cache = serviceProvider.GetRequiredService<IMemoryCache>();
        var telemetry = serviceProvider.GetRequiredService<AspireCliTelemetry>();
        return new NuGetPackageCache(logger, runner, cache, telemetry);
    };

    public Func<IServiceProvider, IAppHostBackchannel> AppHostBackchannelFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AppHostBackchannel>>();
        var telemetry = serviceProvider.GetRequiredService<AspireCliTelemetry>();
        return new AppHostBackchannel(logger, telemetry);
    };

    public Func<IServiceProvider, IFeatures> FeatureFlagsFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return new Features(configuration);
    };

    public Func<IServiceProvider, ITemplateProvider> TemplateProviderFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        var runner = serviceProvider.GetRequiredService<IDotNetCliRunner>();
        var certificateService = serviceProvider.GetRequiredService<ICertificateService>();
        var nuGetPackageCache = serviceProvider.GetRequiredService<INuGetPackageCache>();
        var prompter = serviceProvider.GetRequiredService<INewCommandPrompter>();
        var factory = new DotNetTemplateFactory(interactionService, runner, certificateService, nuGetPackageCache, prompter);
        return new TemplateProvider([factory]);
    };
}

internal sealed class TestOutputTextWriter(ITestOutputHelper outputHelper) : TextWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? message)
    {
        outputHelper.WriteLine(message!);
    }

    public override void Write(string? message)
    {
        outputHelper.Write(message!);
    }

}
