// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DeploymentState;

internal sealed class UserSecretsDeploymentStateProvider(Assembly? appHostAssembly, ILogger<UserSecretsDeploymentStateProvider> logger) : IDeploymentStateProvider
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private static string? GetUserSecretsPath(string userSecretsId)
    {
        return UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
    }

    public async Task<JsonObject> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (appHostAssembly?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is not { } userSecretsId)
        {
            return [];
        }

        var userSecretsPath = GetUserSecretsPath(userSecretsId);
        if (userSecretsPath is null || !File.Exists(userSecretsPath))
        {
            return [];
        }

        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        try
        {
            var content = await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false);
            var secrets = JsonNode.Parse(content, documentOptions: jsonDocumentOptions)?.AsObject() ?? [];

            // Return the entire user secrets content as deployment state for back-compat with run-mode provisioning
            return secrets;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load deployment state from user secrets. Starting with empty state.");
            return [];
        }
    }

    public async Task SaveAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        if (appHostAssembly?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is not { } userSecretsId)
        {
            return;
        }

        var userSecretsPath = GetUserSecretsPath(userSecretsId);
        if (userSecretsPath is null)
        {
            logger.LogWarning("User secrets path could not be determined.");
            return;
        }

        try
        {
            // Ensure directory exists before attempting to create secrets file
            Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);

            // Write the deployment state directly as the entire user secrets file
            await File.WriteAllTextAsync(userSecretsPath, state.ToJsonString(s_jsonSerializerOptions), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Deployment state saved to user secrets.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save deployment state to user secrets.");
        }
    }

    /// <summary>
    /// Loads all user secrets from the current application.
    /// </summary>
    public async Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken = default)
    {
        if (appHostAssembly?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is not { } userSecretsId)
        {
            return [];
        }

        var userSecretsPath = GetUserSecretsPath(userSecretsId);
        if (userSecretsPath is null || !File.Exists(userSecretsPath))
        {
            return [];
        }

        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        try
        {
            var content = await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false);
            var userSecrets = JsonNode.Parse(content, documentOptions: jsonDocumentOptions)?.AsObject() ?? [];
            return userSecrets;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load user secrets.");
            return [];
        }
    }

    /// <summary>
    /// Saves user secrets to the current application.
    /// </summary>
    public async Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        if (appHostAssembly?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is not { } userSecretsId)
        {
            return;
        }

        var userSecretsPath = GetUserSecretsPath(userSecretsId);
        if (userSecretsPath is null)
        {
            logger.LogWarning("User secrets path could not be determined.");
            return;
        }

        try
        {
            // Ensure directory exists before attempting to create secrets file
            Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);

            // Flatten the JsonObject to use colon-separated keys for configuration compatibility
            var flattenedSecrets = FlattenJsonObject(userSecrets);

            await File.WriteAllTextAsync(userSecretsPath, flattenedSecrets.ToJsonString(s_jsonSerializerOptions), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("User secrets saved.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save user secrets.");
        }
    }

    /// <summary>
    /// Flattens a JsonObject to use colon-separated keys for configuration compatibility.
    /// This ensures all secrets are stored in the flat format expected by .NET configuration.
    /// </summary>
    private static JsonObject FlattenJsonObject(JsonObject source)
    {
        var result = new JsonObject();
        FlattenJsonObjectRecursive(source, string.Empty, result);
        return result;
    }

    private static void FlattenJsonObjectRecursive(JsonObject source, string prefix, JsonObject result)
    {
        foreach (var kvp in source)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

            if (kvp.Value is JsonObject nestedObject)
            {
                FlattenJsonObjectRecursive(nestedObject, key, result);
            }
            else if (kvp.Value is JsonArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var arrayKey = $"{key}:{i}";
                    if (array[i] is JsonObject arrayObject)
                    {
                        FlattenJsonObjectRecursive(arrayObject, arrayKey, result);
                    }
                    else
                    {
                        result[arrayKey] = array[i]?.DeepClone();
                    }
                }
            }
            else
            {
                result[key] = kvp.Value?.DeepClone();
            }
        }
    }
}
