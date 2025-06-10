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
    public static IServiceCollection CreateServiceCollection(TemporaryWorkspace workspace, ITestOutputHelper outputHelper, Action<CliServiceCollectionTestOptions>? configure = null)
    {
        var options = new CliServiceCollectionTestOptions(outputHelper);
        options.WorkingDirectory = workspace.WorkspaceRoot;
        configure?.Invoke(options);

        var services = new ServiceCollection();

        // Build configuration for testing
        var configBuilder = new ConfigurationBuilder();
        
        // Add local settings first (if provided)
        if (!string.IsNullOrEmpty(options.LocalSettingsContent))
        {
            var localBytes = Encoding.UTF8.GetBytes(options.LocalSettingsContent);
            var localStream = new MemoryStream(localBytes);
            configBuilder.AddJsonStream(localStream);
        }

        // Then add global settings (if provided) - this will override local settings
        if (!string.IsNullOrEmpty(options.GlobalSettingsContent))
        {
            var globalBytes = Encoding.UTF8.GetBytes(options.GlobalSettingsContent);
            var globalStream = new MemoryStream(globalBytes);
            configBuilder.AddJsonStream(globalStream);
        }
        
        var configuration = configBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging();

        services.AddMemoryCache();

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
        services.AddSingleton(options.ConfigurationWriterFactory);
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

internal sealed class CliServiceCollectionTestOptions
{
    private readonly ITestOutputHelper _outputHelper;

    public CliServiceCollectionTestOptions(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        // Create a unique temporary directory for this test
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);
        WorkingDirectory = new DirectoryInfo(tempPath);
        ProjectLocatorFactory = CreateDefaultProjectLocatorFactory;
        ConfigurationWriterFactory = CreateDefaultConfigurationWriterFactory;
    }

    public string? LocalSettingsContent { get; set; }
    public string? GlobalSettingsContent { get; set; }
    public DirectoryInfo WorkingDirectory { get; set; }
    
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

    public Func<IServiceProvider, IConfigurationWriter> ConfigurationWriterFactory { get; set; }

    public IConfigurationWriter CreateDefaultConfigurationWriterFactory(IServiceProvider serviceProvider)
    {
        return new ConfigurationWriter(WorkingDirectory);
    }

    public Func<IServiceProvider, IProjectLocator> ProjectLocatorFactory { get; set; }

    public IProjectLocator CreateDefaultProjectLocatorFactory(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ProjectLocator>>();
        var runner = serviceProvider.GetRequiredService<IDotNetCliRunner>();
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        var configurationWriter = serviceProvider.GetRequiredService<IConfigurationWriter>();
        return new ProjectLocator(logger, runner, WorkingDirectory, interactionService, configurationWriter);
    }

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
