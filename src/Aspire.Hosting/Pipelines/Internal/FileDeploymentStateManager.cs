// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Pipelines.Internal;

/// <summary>
/// File-based deployment state manager for deployment scenarios.
/// </summary>
internal sealed class FileDeploymentStateManager(
    ILogger<FileDeploymentStateManager> logger,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    IOptions<PipelineOptions> pipelineOptions) : DeploymentStateManagerBase<FileDeploymentStateManager>(logger)
{
    /// <inheritdoc/>
    public override string? StateFilePath => GetStatePath();

    /// <inheritdoc/>
    protected override string? GetStatePath()
    {
        // Use PathSha256 for deployment state to disambiguate projects with the same name in different locations
        var appHostSha = configuration["AppHost:PathSha256"];
        if (string.IsNullOrEmpty(appHostSha))
        {
            return null;
        }

        var environment = hostEnvironment.EnvironmentName.ToLowerInvariant();
        var aspireDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "deployments",
            appHostSha
        );

        return Path.Combine(aspireDir, $"{environment}.json");
    }

    /// <inheritdoc/>
    protected override async Task SaveStateToStorageAsync(JsonObject state, CancellationToken cancellationToken)
    {
        try
        {
            if (pipelineOptions.Value.ClearCache)
            {
                logger.LogInformation("Skipping deployment state save due to --clear-cache flag");
                return;
            }

            var deploymentStatePath = GetStatePath();
            if (deploymentStatePath is null)
            {
                logger.LogWarning("Cannot save deployment state: AppHostSha is not configured");
                return;
            }

            var flattenedSecrets = JsonFlattener.FlattenJsonObject(state);
            var deploymentStateDirectory = Path.GetDirectoryName(deploymentStatePath)!;
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(deploymentStateDirectory);
            }
            else
            {
                var expectedMode = UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead;
                if (Directory.Exists(deploymentStateDirectory))
                {
                    try
                    {
                        var currentMode = File.GetUnixFileMode(deploymentStateDirectory);
                        if ((currentMode & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                                            UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
                        {
                            logger.LogWarning(
                                "Deployment state directory '{Directory}' has permissions that allow access to other users. " +
                                "Consider restricting permissions to the current user only by running: chmod 700 {Directory}",
                                deploymentStateDirectory,
                                deploymentStateDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Unable to check permissions on deployment state directory '{Directory}'.", deploymentStateDirectory);
                    }
                }
                else
                {
                    Directory.CreateDirectory(deploymentStateDirectory, expectedMode);
                }
            }
            await File.WriteAllTextAsync(
                deploymentStatePath,
                flattenedSecrets.ToJsonString(s_jsonSerializerOptions),
                cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Deployment state saved to {Path}", deploymentStatePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save deployment state.");
            throw;
        }
    }
}
