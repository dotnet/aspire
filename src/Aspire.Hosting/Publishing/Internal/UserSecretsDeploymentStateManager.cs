// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing.Internal;

/// <summary>
/// User secrets implementation of <see cref="IDeploymentStateManager"/>.
/// </summary>
public sealed class UserSecretsDeploymentStateManager(ILogger<UserSecretsDeploymentStateManager> logger) : IDeploymentStateManager
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc/>
    public string? StateFilePath => GetUserSecretsPath();

    private static string? GetUserSecretsPath()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId switch
        {
            null => Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID"),
            string id => UserSecretsPathHelper.GetSecretsPathFromSecretsId(id)
        };
    }

    /// <inheritdoc/>
    public async Task<JsonObject> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        var userSecretsPath = GetUserSecretsPath();
        var userSecrets = userSecretsPath is not null && File.Exists(userSecretsPath)
            ? JsonNode.Parse(await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false),
                documentOptions: jsonDocumentOptions)!.AsObject()
            : [];
        return userSecrets;
    }

    /// <inheritdoc/>
    public async Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        try
        {
            var userSecretsPath = GetUserSecretsPath();
            if (userSecretsPath is null)
            {
                throw new InvalidOperationException("User secrets path could not be determined.");
            }

            // Load current state from disk to merge with incoming state
            var currentState = await LoadStateAsync(cancellationToken).ConfigureAwait(false);

            // Merge incoming state into current state (preserves concurrent writes)
            MergeJsonObjects(currentState, state);

            var flattenedUserSecrets = FlattenJsonObject(currentState);
            Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);
            await File.WriteAllTextAsync(userSecretsPath, flattenedUserSecrets.ToJsonString(s_jsonSerializerOptions), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Azure resource connection strings saved to user secrets.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to provision Azure resources because user secrets file is not well-formed JSON.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save user secrets.");
        }
    }

    /// <summary>
    /// Merges properties from source JsonObject into target JsonObject, overwriting existing keys.
    /// </summary>
    private static void MergeJsonObjects(JsonObject target, JsonObject source)
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value?.DeepClone();
        }
    }

    /// <summary>
    /// Flattens a JsonObject to use colon-separated keys for configuration compatibility.
    /// This ensures all secrets are stored in the flat format expected by .NET configuration.
    /// </summary>
    public static JsonObject FlattenJsonObject(JsonObject source)
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
                // Flatten arrays using index-based keys (standard .NET configuration format)
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
