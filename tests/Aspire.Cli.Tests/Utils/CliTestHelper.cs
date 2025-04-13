// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Certificates;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Tests.Utils;

internal static class CliTestHelper
{
    public static IServiceCollection CreateServiceCollection(Action<CliServiceCollectionTestOptions>? configure = null)
    {
        var options = new CliServiceCollectionTestOptions();
        configure?.Invoke(options);

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(options.ProjectLocatorFactory);
        services.AddSingleton(options.InteractiveServiceFactory);
        services.AddSingleton(options.CertificateServiceFactory);
        services.AddSingleton(options.NewCommandPrompterFactory);
        services.AddTransient(options.DotNetCliRunnerFactory);
        services.AddTransient(options.NuGetPackageCacheFactory);
        services.AddTransient<RootCommand>();
        services.AddTransient<NewCommand>();
        services.AddTransient<RunCommand>();
        services.AddTransient<AddCommand>();
        services.AddTransient<PublishCommand>();

        return services;
    }
}

internal sealed class CliServiceCollectionTestOptions
{
    public Func<IServiceProvider, INewCommandPrompter> NewCommandPrompterFactory { get; set; } = (IServiceProvider serviceProvider) =>
    {
        var interactionService = serviceProvider.GetRequiredService<IInteractionService>();
        return new NewCommandPrompter(interactionService);
    };

    public Func<IServiceProvider, IProjectLocator> ProjectLocatorFactory { get; set; } = (IServiceProvider serviceProvider) => {
        var logger = serviceProvider.GetRequiredService<ILogger<ProjectLocator>>();
        return new ProjectLocator(logger, Directory.GetCurrentDirectory());
    };

    public Func<IServiceProvider, IInteractionService> InteractiveServiceFactory { get; set; } = (IServiceProvider serviceProvider) => {
        return new InteractionService();
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
        return new NuGetPackageCache(logger, runner);
    };
}
