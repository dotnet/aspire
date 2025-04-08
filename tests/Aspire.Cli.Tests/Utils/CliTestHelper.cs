// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
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