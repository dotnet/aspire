// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing.Internal;

/// <summary>
/// File-based deployment state manager for publish scenarios.
/// </summary>
public sealed class FileDeploymentStateManager(
    ILogger<FileDeploymentStateManager> logger,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    IOptions<PublishingOptions> publishingOptions) : IDeploymentStateManager
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc/>
    public string? StateFilePath => GetDeploymentStatePath();

    private string? GetDeploymentStatePath()
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
    public async Task<JsonObject> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        var deploymentStatePath = GetDeploymentStatePath();

        if (deploymentStatePath is not null && File.Exists(deploymentStatePath))
        {
            logger.LogInformation("Loading deployment state from {Path}", deploymentStatePath);
            return JsonNode.Parse(
                await File.ReadAllTextAsync(deploymentStatePath, cancellationToken).ConfigureAwait(false),
                documentOptions: jsonDocumentOptions)!.AsObject();
        }

        return [];
    }

    /// <inheritdoc/>
    public async Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        try
        {
            if (publishingOptions.Value.ClearCache)
            {
                logger.LogInformation("Skipping deployment state save due to --clear-cache flag");
                return;
            }

            var deploymentStatePath = GetDeploymentStatePath();
            if (deploymentStatePath is null)
            {
                logger.LogWarning("Cannot save deployment state: AppHostSha is not configured");
                return;
            }

            var flattenedSecrets = FlattenJsonObject(state);
            Directory.CreateDirectory(Path.GetDirectoryName(deploymentStatePath)!);
            await File.WriteAllTextAsync(
                deploymentStatePath,
                flattenedSecrets.ToJsonString(s_jsonSerializerOptions),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Deployment state saved to {Path}", deploymentStatePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save deployment state.");
        }
    }

    private static JsonObject FlattenJsonObject(JsonObject input)
    {
        var result = new JsonObject();

        void Flatten(JsonObject obj, string prefix)
        {
            foreach (var kvp in obj)
            {
                var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

                if (kvp.Value is JsonObject nestedObj)
                {
                    Flatten(nestedObj, key);
                }
                else
                {
                    result[key] = kvp.Value?.DeepClone();
                }
            }
        }

        Flatten(input, string.Empty);
        return result;
    }
}
