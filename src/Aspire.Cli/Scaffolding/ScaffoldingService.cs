// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
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
    private readonly CliExecutionContext _executionContext;
    private readonly ILogger<ScaffoldingService> _logger;

    public ScaffoldingService(
        IAppHostServerProjectFactory appHostServerProjectFactory,
        ILanguageDiscovery languageDiscovery,
        IInteractionService interactionService,
        IPackagingService packagingService,
        IConfigurationService configurationService,
        CliExecutionContext executionContext,
        ILogger<ScaffoldingService> logger)
    {
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _languageDiscovery = languageDiscovery;
        _interactionService = interactionService;
        _packagingService = packagingService;
        _configurationService = configurationService;
        _executionContext = executionContext;
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

        // Step 1: Resolve SDK version and channel (prompts user if multiple hives exist)
        var (sdkVersion, channel) = await ResolveSdkVersionAndChannelAsync(context.Channel, cancellationToken);
        
        // Load or create config with resolved SDK version
        var config = AspireJsonConfiguration.LoadOrCreate(directory.FullName, sdkVersion);

        // Include the code generation package for scaffolding and code gen
        var codeGenPackage = await _languageDiscovery.GetPackageForLanguageAsync(language.LanguageId, cancellationToken);
        var packages = config.GetAllPackages().ToList();
        if (codeGenPackage is not null)
        {
            packages.Add((codeGenPackage, config.SdkVersion!));
        }

        var appHostServerProject = _appHostServerProjectFactory.Create(directory.FullName);
        var socketPath = appHostServerProject.GetSocketPath();

        var (buildSuccess, buildOutput, channelName) = await _interactionService.ShowStatusAsync(
            ":gear:  Preparing Aspire server...",
            () => BuildAppHostServerAsync(appHostServerProject, config.SdkVersion!, packages, channel, cancellationToken));
        if (!buildSuccess)
        {
            _interactionService.DisplayLines(buildOutput.GetLines());
            _interactionService.DisplayError("Failed to build AppHost server.");
            return;
        }

        // Step 2: Start the server temporarily for scaffolding and code generation
        var currentPid = Environment.ProcessId;
        var (serverProcess, _) = appHostServerProject.Run(socketPath, currentPid, new Dictionary<string, string>());

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
                $":package:  Installing {language.DisplayName} dependencies...",
                () => InstallDependenciesAsync(directory, language, rpcClient, cancellationToken));
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
            if (channelName is not null)
            {
                config.Channel = channelName;
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

    private static async Task<(bool Success, OutputCollector Output, string? ChannelName)> BuildAppHostServerAsync(
        AppHostServerProject appHostServerProject,
        string sdkVersion,
        List<(string Name, string Version)> packages,
        PackageChannel? channel,
        CancellationToken cancellationToken)
    {
        var outputCollector = new OutputCollector();

        var (_, channelName) = await appHostServerProject.CreateProjectFilesAsync(sdkVersion, packages, cancellationToken, channel: channel);
        var (buildSuccess, buildOutput) = await appHostServerProject.BuildAsync(cancellationToken);
        if (!buildSuccess)
        {
            foreach (var (_, line) in buildOutput.GetLines())
            {
                outputCollector.AppendOutput(line);
            }
        }

        return (buildSuccess, outputCollector, channelName);
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
    /// Resolves the SDK version and channel to use for scaffolding.
    /// Checks for channel from CLI option, global config, or prompts user if multiple hives exist.
    /// </summary>
    /// <param name="inputChannel">Channel name from CLI option (highest priority).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SDK version to use and the resolved channel (null for implicit/default channel).</returns>
    private async Task<(string SdkVersion, PackageChannel? Channel)> ResolveSdkVersionAndChannelAsync(string? inputChannel, CancellationToken cancellationToken)
    {
        var allChannels = await _packagingService.GetChannelsAsync(cancellationToken);

        // Check if channel was provided via CLI option (highest priority)
        var channelName = inputChannel;

        // If no channel from CLI, check for global channel setting
        if (string.IsNullOrEmpty(channelName))
        {
            channelName = await _configurationService.GetConfigurationAsync("channel", cancellationToken);
        }

        PackageChannel? channel;

        if (!string.IsNullOrEmpty(channelName))
        {
            // Find the matching channel by name
            channel = allChannels.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));
            if (channel is null)
            {
                _logger.LogWarning("Configured channel '{Channel}' not found, using default SDK version", channelName);
                return (AppHostServerProject.DefaultSdkVersion, null);
            }
        }
        else
        {
            // If there are hives (PR build directories), prompt for channel selection.
            // Otherwise, use the implicit/default channel automatically.
            var hasHives = _executionContext.GetPrHiveCount() > 0;

            if (hasHives)
            {
                // Prompt for channel selection
                channel = await _interactionService.PromptForSelectionAsync(
                    Resources.UpdateCommandStrings.SelectChannelPrompt,
                    allChannels,
                    (c) => $"{c.Name} ({c.SourceDetails})",
                    cancellationToken);
            }
            else
            {
                // Use the default (implicit) channel, which means no specific version override
                return (AppHostServerProject.DefaultSdkVersion, null);
            }
        }

        // Get template packages from the channel to determine SDK version
        var templatePackages = await channel.GetTemplatePackagesAsync(new DirectoryInfo(Environment.CurrentDirectory), cancellationToken);
        var latestPackage = templatePackages
            .OrderByDescending(p => SemVersion.Parse(p.Version), SemVersion.PrecedenceComparer)
            .FirstOrDefault();

        if (latestPackage is null)
        {
            _logger.LogWarning("No packages found in channel '{Channel}', using default SDK version", channel.Name);
            return (AppHostServerProject.DefaultSdkVersion, channel);
        }

        _logger.LogDebug("Resolved SDK version {Version} from channel {Channel}", latestPackage.Version, channel.Name);
        return (latestPackage.Version, channel);
    }
}
