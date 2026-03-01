// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Scaffolding;

/// <summary>
/// Service for scaffolding new AppHost projects.
/// Handles both C# (via templates) and guest languages (via RPC).
/// </summary>
internal sealed class ScaffoldingService : IScaffoldingService
{
    private readonly IAppHostServerProjectFactory _appHostServerProjectFactory;
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly IInteractionService _interactionService;
    private readonly IPackagingService _packagingService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ScaffoldingService> _logger;

    public ScaffoldingService(
        IAppHostServerProjectFactory appHostServerProjectFactory,
        ILanguageDiscovery languageDiscovery,
        IInteractionService interactionService,
        IPackagingService packagingService,
        IConfigurationService configurationService,
        ILogger<ScaffoldingService> logger)
    {
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _languageDiscovery = languageDiscovery;
        _interactionService = interactionService;
        _packagingService = packagingService;
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ScaffoldAsync(ScaffoldContext context, CancellationToken cancellationToken)
    {
        if (context.Language.LanguageId.Value.Equals(KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("C# projects should be created using the template system via NewCommand.");
        }

        await ScaffoldGuestLanguageAsync(context, cancellationToken);
    }

    private async Task ScaffoldGuestLanguageAsync(ScaffoldContext context, CancellationToken cancellationToken)
    {
        var directory = context.TargetDirectory;
        var language = context.Language;

        // Step 1: Resolve SDK and package strategy
        var sdkVersion = await ResolveSdkVersionAsync(cancellationToken);
        var config = AspireJsonConfiguration.LoadOrCreate(directory.FullName, sdkVersion);

        // Include the code generation package for scaffolding and code gen
        var codeGenPackage = await _languageDiscovery.GetPackageForLanguageAsync(language.LanguageId, cancellationToken);
        var packages = config.GetAllPackages(sdkVersion).ToList();
        if (codeGenPackage is not null)
        {
            var codeGenVersion = config.GetEffectiveSdkVersion(sdkVersion);
            packages.Add((codeGenPackage, codeGenVersion));
        }

        var appHostServerProject = await _appHostServerProjectFactory.CreateAsync(directory.FullName, cancellationToken);
        var prepareSdkVersion = config.GetEffectiveSdkVersion(sdkVersion);

        var prepareResult = await _interactionService.ShowStatusAsync(
            "Preparing Aspire server...",
            () => appHostServerProject.PrepareAsync(prepareSdkVersion, packages, cancellationToken),
            emoji: KnownEmojis.Gear);
        if (!prepareResult.Success)
        {
            if (prepareResult.Output is not null)
            {
                _interactionService.DisplayLines(prepareResult.Output.GetLines());
            }
            _interactionService.DisplayError("Failed to build AppHost server.");
            return;
        }

        // Step 2: Start the server temporarily for scaffolding and code generation
        var currentPid = Environment.ProcessId;
        var (socketPath, serverProcess, _) = appHostServerProject.Run(currentPid, new Dictionary<string, string>());

        try
        {
            // Step 3: Connect to server and get scaffold templates via RPC
            await using var rpcClient = await AppHostRpcClient.ConnectAsync(socketPath, cancellationToken);

            var scaffoldFiles = await rpcClient.ScaffoldAppHostAsync(
                language.LanguageId,
                directory.FullName,
                context.ProjectName,
                cancellationToken);

            // Step 4: Write scaffold files to disk
            foreach (var (fileName, content) in scaffoldFiles)
            {
                var filePath = Path.Combine(directory.FullName, fileName);
                var fileDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }
                await File.WriteAllTextAsync(filePath, content, cancellationToken);
            }

            _logger.LogDebug("Wrote {Count} scaffold files", scaffoldFiles.Count);

            // Step 5: Install dependencies using GuestRuntime
            var installResult = await _interactionService.ShowStatusAsync(
                $"Installing {language.DisplayName} dependencies...",
                () => InstallDependenciesAsync(directory, language, rpcClient, cancellationToken),
                emoji: KnownEmojis.Package);
            if (installResult != 0)
            {
                return;
            }

            // Step 6: Generate SDK code via RPC
            await GenerateCodeViaRpcAsync(
                directory.FullName,
                rpcClient,
                language,
                cancellationToken);

            // Save channel and language to settings.json
            if (prepareResult.ChannelName is not null)
            {
                config.Channel = prepareResult.ChannelName;
            }

            config.Language = language.LanguageId;
            config.Save(directory.FullName);
        }
        finally
        {
            // Step 7: Stop the server
            if (!serverProcess.HasExited)
            {
                try
                {
                    serverProcess.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process after scaffolding");
                }
            }
        }
    }

    private async Task<int> InstallDependenciesAsync(
        DirectoryInfo directory,
        LanguageInfo language,
        IAppHostRpcClient rpcClient,
        CancellationToken cancellationToken)
    {
        var runtimeSpec = await rpcClient.GetRuntimeSpecAsync(language.LanguageId.Value, cancellationToken);
        var runtime = new GuestRuntime(runtimeSpec, _logger);

        var result = await runtime.InstallDependenciesAsync(directory, cancellationToken);
        if (result != 0)
        {
            _interactionService.DisplayError($"Failed to install {language.DisplayName} dependencies.");
        }

        return result;
    }

    private const string GeneratedFolderName = ".modules";

    private async Task GenerateCodeViaRpcAsync(
        string directoryPath,
        IAppHostRpcClient rpcClient,
        LanguageInfo language,
        CancellationToken cancellationToken)
    {
        var generatedFiles = await rpcClient.GenerateCodeAsync(language.CodeGenerator, cancellationToken);

        // Write generated files to the output directory
        var outputPath = Path.Combine(directoryPath, GeneratedFolderName);
        Directory.CreateDirectory(outputPath);

        foreach (var (fileName, content) in generatedFiles)
        {
            var filePath = Path.Combine(outputPath, fileName);
            var fileDirectory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }
            await File.WriteAllTextAsync(filePath, content, cancellationToken);
        }

        _logger.LogDebug("Generated {Count} code files in {Path}", generatedFiles.Count, outputPath);
    }

    /// <summary>
    /// Resolves the SDK version to use for scaffolding.
    /// If a channel is configured globally, queries that channel for available versions.
    /// Otherwise, falls back to the default SDK version.
    /// </summary>
    private async Task<string> ResolveSdkVersionAsync(CancellationToken cancellationToken)
    {
        // Check for global channel setting
        var channelName = await _configurationService.GetConfigurationAsync("channel", cancellationToken);
        if (string.IsNullOrEmpty(channelName))
        {
            return DotNetBasedAppHostServerProject.DefaultSdkVersion;
        }

        // Find the matching channel
        var allChannels = await _packagingService.GetChannelsAsync(cancellationToken);
        var channel = allChannels.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));
        if (channel is null)
        {
            _logger.LogWarning("Configured channel '{Channel}' not found, using default SDK version", channelName);
            return DotNetBasedAppHostServerProject.DefaultSdkVersion;
        }

        // Get template packages from the channel to determine SDK version
        var templatePackages = await channel.GetTemplatePackagesAsync(new DirectoryInfo(Environment.CurrentDirectory), cancellationToken);
        var latestPackage = templatePackages
            .OrderByDescending(p => SemVersion.Parse(p.Version), SemVersion.PrecedenceComparer)
            .FirstOrDefault();

        if (latestPackage is null)
        {
            _logger.LogWarning("No packages found in channel '{Channel}', using default SDK version", channelName);
            return DotNetBasedAppHostServerProject.DefaultSdkVersion;
        }

        _logger.LogDebug("Resolved SDK version {Version} from channel {Channel}", latestPackage.Version, channelName);
        return latestPackage.Version;
    }
}
