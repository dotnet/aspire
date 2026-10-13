// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Bundles;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Layout;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Factory for creating AppHostServerProject instances with required dependencies.
/// </summary>
internal interface IAppHostServerProjectFactory
{
    Task<IAppHostServerProject> CreateAsync(string appPath, CancellationToken cancellationToken = default);
    Task<IAppHostServerProject> CreateAsync(string appPath, string? restoreRootConfigDirectory, CancellationToken cancellationToken);
}

/// <summary>
/// Factory implementation that creates IAppHostServerProject instances.
/// Chooses between DotNetBasedAppHostServerProject (dev mode) and PrebuiltAppHostServer (bundle mode).
/// </summary>
internal sealed class AppHostServerProjectFactory(
    IDotNetCliRunner dotNetCliRunner,
    IPackagingService packagingService,
    IBundleService bundleService,
    BundleNuGetService bundleNuGetService,
    IDotNetSdkInstaller sdkInstaller,
    CliExecutionContext executionContext,
    IEnvironment environment,
    IProcessExecutionFactory processExecutionFactory,
    ILoggerFactory loggerFactory) : IAppHostServerProjectFactory
{
    public Task<IAppHostServerProject> CreateAsync(string appPath, CancellationToken cancellationToken = default)
        => CreateAsync(appPath, restoreRootConfigDirectory: null, cancellationToken);

    public async Task<IAppHostServerProject> CreateAsync(string appPath, string? restoreRootConfigDirectory, CancellationToken cancellationToken)
    {
        var configDirectory = ConfigurationHelper.GetConfigRootDirectory(new DirectoryInfo(appPath));
        if (KnownFeatures.IsAppHostServerV2Enabled(AspireConfigFile.Load(configDirectory.FullName)))
        {
            return await CreateAppHostServerV2Async(appPath, cancellationToken).ConfigureAwait(false);
        }

        var socketPath = CliPathHelper.CreateGuestAppHostSocketPath("apphost.sock");

        // Priority 1: Check for dev mode (ASPIRE_REPO_ROOT or running from Aspire source repo)
        var repoRoot = AspireRepositoryDetector.DetectRepositoryRoot(appPath);
        if (repoRoot is not null)
        {
            return new DotNetBasedAppHostServerProject(
                appPath,
                socketPath,
                repoRoot,
                dotNetCliRunner,
                packagingService,
                processExecutionFactory,
                environment,
                loggerFactory.CreateLogger<DotNetBasedAppHostServerProject>(),
                logFilePath: executionContext.LogFilePath,
                restoreRootConfigDirectory: restoreRootConfigDirectory);
        }

        // Priority 2: Ensure bundle is extracted and check for layout
        var layoutLease = await bundleService.EnsureExtractedAndAcquireLayoutAsync("cli", "apphost-server", cancellationToken);
        var layout = layoutLease?.Layout;

        // Priority 3: Check if we have a bundle layout with a pre-built AppHost server
        if (layout is not null && layout.GetManagedPath() is string serverPath && File.Exists(serverPath))
        {
            return CreatePrebuiltAppHostServer(appPath, socketPath, layout, layoutLease);
        }

        layoutLease?.Dispose();
        throw new InvalidOperationException(
            "No Aspire AppHost server is available. Ensure the Aspire CLI is installed " +
            "with a valid bundle layout, or reinstall using 'aspire setup --force'.");
    }

    private async Task<IAppHostServerProject> CreateAppHostServerV2Async(string appPath, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("AppHost Server v2 currently requires Unix-domain sockets.");
        }

        // Overriding the server executable still requires available bundle tooling
        // for provider SDK preparation and bundled runtime components.
        var lease = await bundleService.EnsureExtractedAndAcquireLayoutAsync("cli", "apphost-server-v2", cancellationToken).ConfigureAwait(false);
        try
        {
            var path = environment.GetEnvironmentVariable(NativeAppHostServerProject.ExecutableEnvironmentVariable);
            if (string.IsNullOrEmpty(path))
            {
                path = lease?.Layout.GetAppHostServerPath();
                if (path is null || !File.Exists(path))
                {
                    throw new InvalidOperationException(
                        "This Aspire bundle does not contain AppHost Server v2. Update or reinstall the CLI, " +
                        "or disable features.appHostServerV2 in this AppHost's aspire.config.json.");
                }
            }

            return new NativeAppHostServerProject(appPath, path, processExecutionFactory,
                loggerFactory.CreateLogger<NativeAppHostServerProject>())
            {
                LayoutLease = lease
            };
        }
        catch
        {
            lease?.Dispose();
            throw;
        }
    }

    internal PrebuiltAppHostServer CreatePrebuiltAppHostServer(
        string appPath,
        string socketPath,
        LayoutConfiguration layout,
        BundleLayoutLease? layoutLease)
    {
        try
        {
            return new PrebuiltAppHostServer(
                appPath,
                socketPath,
                layout,
                bundleNuGetService,
                dotNetCliRunner,
                sdkInstaller,
                packagingService,
                executionContext,
                processExecutionFactory,
                environment,
                loggerFactory.CreateLogger<PrebuiltAppHostServer>(),
                layoutLease);
        }
        catch
        {
            layoutLease?.Dispose();
            throw;
        }
    }
}
