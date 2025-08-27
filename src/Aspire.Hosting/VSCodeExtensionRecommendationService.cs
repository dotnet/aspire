// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Aspire.Hosting;

/// <summary>
/// A background service that recommends Visual Studio Code extensions based on resource annotations.
/// </summary>
internal sealed class VSCodeExtensionRecommendationService : BackgroundService
{
    private readonly DistributedApplicationModel _applicationModel;
    private readonly IInteractionService _interactionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VSCodeExtensionRecommendationService> _logger;

    public VSCodeExtensionRecommendationService(
        DistributedApplicationModel applicationModel,
        IInteractionService interactionService,
        IConfiguration configuration,
        ILogger<VSCodeExtensionRecommendationService> logger)
    {
        _applicationModel = applicationModel;
        _interactionService = interactionService;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Small delay to let the application start up
            await Task.Delay(2000, stoppingToken).ConfigureAwait(false);

            // Check if running in VSCode terminal
            if (!IsRunningInVSCodeTerminal())
            {
                _logger.LogDebug("Not running in VSCode terminal, skipping extension recommendations");
                return;
            }

            // Check if Aspire VSCode extension is installed
            var aspireExtensionInstalled = await IsExtensionInstalledAsync("ms-dotnettools.aspire", stoppingToken).ConfigureAwait(false);

            // Aggregate all VSCode extension annotations from resources
            var extensionAnnotations = GetAllVSCodeExtensionAnnotations();

            if (!aspireExtensionInstalled || extensionAnnotations.Count > 0)
            {
                await RecommendExtensionsAsync(aspireExtensionInstalled, extensionAnnotations, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing VSCode extension recommendations");
        }
    }

    private bool IsRunningInVSCodeTerminal()
    {
        // Check various environment variables that indicate VSCode
        return !string.IsNullOrEmpty(_configuration.GetValue<string>("TERM_PROGRAM")) &&
               _configuration.GetValue<string>("TERM_PROGRAM") == "vscode" ||
               !string.IsNullOrEmpty(_configuration.GetValue<string>("VSCODE_IPC_HOOK_CLI")) ||
               !string.IsNullOrEmpty(_configuration.GetValue<string>("VSCODE_PID"));
    }

    private async Task<bool> IsExtensionInstalledAsync(string extensionId, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "code",
                Arguments = "--list-extensions",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start 'code' process to check installed extensions");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("'code --list-extensions' exited with code {ExitCode}", process.ExitCode);
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var installedExtensions = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            return installedExtensions.Contains(extensionId, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if extension {ExtensionId} is installed", extensionId);
            return false;
        }
    }

    private List<VisualStudioCodeExtensionAnnotation> GetAllVSCodeExtensionAnnotations()
    {
        var annotations = new List<VisualStudioCodeExtensionAnnotation>();

        foreach (var resource in _applicationModel.Resources)
        {
            annotations.AddRange(resource.Annotations.OfType<VisualStudioCodeExtensionAnnotation>());
        }

        return annotations.DistinctBy(a => a.Id).ToList();
    }

    private async Task RecommendExtensionsAsync(
        bool aspireExtensionInstalled,
        List<VisualStudioCodeExtensionAnnotation> extensionAnnotations,
        CancellationToken cancellationToken)
    {
        var extensionsToRecommend = new List<VisualStudioCodeExtensionAnnotation>();

        // Always recommend Aspire extension if not installed
        if (!aspireExtensionInstalled)
        {
            extensionsToRecommend.Add(new VisualStudioCodeExtensionAnnotation(
                "ms-dotnettools.aspire",
                "Aspire",
                "Enhanced support for .NET Aspire applications"));
        }

        // Filter out already installed extensions
        foreach (var annotation in extensionAnnotations)
        {
            if (!await IsExtensionInstalledAsync(annotation.Id, cancellationToken).ConfigureAwait(false))
            {
                extensionsToRecommend.Add(annotation);
            }
        }

        if (extensionsToRecommend.Count == 0)
        {
            return;
        }

        await PromptUserToInstallExtensionsAsync(extensionsToRecommend, cancellationToken).ConfigureAwait(false);
    }

    private async Task PromptUserToInstallExtensionsAsync(
        List<VisualStudioCodeExtensionAnnotation> extensionsToRecommend,
        CancellationToken cancellationToken)
    {
        if (!_interactionService.IsAvailable)
        {
            _logger.LogInformation("Interaction service is not available, cannot prompt for extension installation");
            return;
        }

        try
        {
            var title = "VSCode Extension Recommendations";
            var message = extensionsToRecommend.Count == 1
                ? $"We recommend installing the following VSCode extension to improve your development experience:\n\n• {extensionsToRecommend[0].DisplayName}"
                : $"We recommend installing the following VSCode extensions to improve your development experience:\n\n{string.Join("\n", extensionsToRecommend.Select(e => $"• {e.DisplayName}"))}";

            var result = await _interactionService.PromptConfirmationAsync(
                title,
                message,
                new MessageBoxInteractionOptions
                {
                    Intent = MessageIntent.Information,
                    PrimaryButtonText = "Install Extensions",
                    SecondaryButtonText = "Skip"
                },
                cancellationToken).ConfigureAwait(false);

            if (!result.Canceled && result.Data)
            {
                await InstallExtensionsAsync(extensionsToRecommend, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error prompting user to install extensions");
        }
    }

    private async Task InstallExtensionsAsync(
        List<VisualStudioCodeExtensionAnnotation> extensions,
        CancellationToken cancellationToken)
    {
        foreach (var extension in extensions)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "code",
                    Arguments = $"--install-extension {extension.Id}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogWarning("Failed to start 'code' process to install extension {ExtensionId}", extension.Id);
                    continue;
                }

                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully installed VSCode extension: {ExtensionDisplayName} ({ExtensionId})",
                        extension.DisplayName, extension.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to install VSCode extension {ExtensionId}, exit code: {ExitCode}",
                        extension.Id, process.ExitCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error installing extension {ExtensionId}", extension.Id);
            }
        }
    }
}