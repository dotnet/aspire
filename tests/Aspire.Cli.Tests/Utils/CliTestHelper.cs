// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Projects;
using Aspire.Cli.Templating;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Aspire.Cli.Configuration;
using Xunit;

namespace Aspire.Cli.Tests.Utils;

internal static class CliTestHelper
{
    public static IServiceCollection CreateServiceCollection(ITestOutputHelper outputHelper, Action<CliServiceCollectionTestOptions>? configure = null)
    {
        var options = new CliServiceCollectionTestOptions(outputHelper);
        configure?.Invoke(options);

        var services = new ServiceCollection();

        // Build configuration similar to Program.cs but for testing
        var configBuilder = new ConfigurationBuilder();
        
        // Add global settings if exists
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalSettingsPath = Path.Combine(homeDirectory, ".aspire", "settings.json");
        if (File.Exists(globalSettingsPath))
        {
            configBuilder.AddJsonFile(globalSettingsPath, optional: true);
        }
        
        // Add local settings files by walking up directory tree
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        var settingsFiles = new List<FileInfo>();
        
        while (currentDirectory is not null)
        {
            var settingsFilePath = Path.Combine(currentDirectory.FullName, ".aspire", "settings.json");
            if (File.Exists(settingsFilePath))
            {
                settingsFiles.Add(new FileInfo(settingsFilePath));
            }
            currentDirectory = currentDirectory.Parent;
        }
        
        // Add in reverse order so closer files take precedence
        settingsFiles.Reverse();
        foreach (var settingsFile in settingsFiles)
        {
            configBuilder.AddJsonFile(settingsFile.FullName, optional: true);
        }
        
        var configuration = configBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging();

        services.AddMemoryCache();

        services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();

        services.AddSingleton(options.AnsiConsoleFactory);
        services.AddSingleton(options.ProjectLocatorFactory);
        services.AddSingleton(options.InteractionServiceFactory);
        services.AddSingleton(options.CertificateServiceFactory);
        services.AddSingleton(options.NewCommandPrompterFactory);
        services.AddSingleton(options.AddCommandPrompterFactory);
        services.AddSingleton(options.PublishCommandPrompterFactory);
        services.AddTransient(options.DotNetCliRunnerFactory);
        services.AddTransient(options.NuGetPackageCacheFactory);
        services.AddSingleton(options.TemplateProviderFactory);
        services.AddTransient<RootCommand>();
        services.AddTransient<NewCommand>();
        services.AddTransient<RunCommand>();
        services.AddTransient<AddCommand>();
        services.AddTransient<PublishCommand>();
        services.AddTransient<ConfigCommand>();
        services.AddTransient(options.AppHostBackchannelFactory);

        return services;
    }
}

internal sealed class CliServiceCollectionTestOptions(ITestOutputHelper outputHelper)
{
    public Func<IServiceProvider, IAnsiConsole> AnsiConsoleFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        AnsiConsoleSettings settings = new AnsiConsoleSettings()
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            ColorSystem = ColorSystemSupport.Standard,
            Out = new AnsiConsoleOutput(new TestOutputTextWriter(outputHelper))
        };
        var ansiConsole = AnsiConsole.Create(settings);
        return ansiConsole;
    };

    public Func<IServiceProvider, INewCommandPrompter> NewCommandPrompterFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new NewCommandPrompter(interactionService);
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

    public Func<IServiceProvider, IProjectLocator> ProjectLocatorFactory { get; set; } = (IServiceProvider serviceProvider) => {
        var logger = serviceProvider.GetRequiredService<ILogger<ProjectLocator>>();
        var runner = serviceProvider.GetRequiredService<IDotNetCliRunner>();
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new ProjectLocator(logger, runner, new DirectoryInfo(Directory.GetCurrentDirectory()), interactionService);
    };

    public Func<IServiceProvider, IInteractionService> InteractionServiceFactory { get; set; } = (IServiceProvider serviceProvider) => {
        var ansiConsole = serviceProvider.GetRequiredService<IAnsiConsole>();
        return new InteractionService(ansiConsole);
    };

    public Func<IServiceProvider, ICertificateService> CertificateServiceFactory { get; set; } = (IServiceProvider serviceProvider) => {
        var interactiveService = serviceProvider.GetRequiredService<IInteractionService>();
        return new CertificateService(interactiveService);
    };

    public Func<IServiceProvider, IDotNetCliRunner> DotNetCliRunnerFactory { get; set; } = (IServiceProvider serviceProvider) => {
        var logger = serviceProvider.GetRequiredService<ILogger<DotNetCliRunner>>();
        return new DotNetCliRunner(logger, serviceProvider);
    };

    public Func<IServiceProvider, INuGetPackageCache> NuGetPackageCacheFactory { get; set; } = (IServiceProvider serviceProvider) => {
        var logger = serviceProvider.GetRequiredService<ILogger<NuGetPackageCache>>();
        var runner = serviceProvider.GetRequiredService<IDotNetCliRunner>();
        var cache = serviceProvider.GetRequiredService<IMemoryCache>();
        return new NuGetPackageCache(logger, runner, cache);
    };

    public Func<IServiceProvider, IAppHostBackchannel> AppHostBackchannelFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AppHostBackchannel>>();
        var rpcTarget = serviceProvider.GetService<CliRpcTarget>() ?? throw new InvalidOperationException("CliRpcTarget not registered");
        return new AppHostBackchannel(logger, rpcTarget);
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

internal sealed class  TestOutputTextWriter(ITestOutputHelper outputHelper) : TextWriter
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
