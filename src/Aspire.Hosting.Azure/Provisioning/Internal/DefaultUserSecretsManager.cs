// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IUserSecretsManager"/>.
/// </summary>
internal sealed class DefaultUserSecretsManager(ILogger<DefaultUserSecretsManager> logger) : IUserSecretsManager
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    // Static lock to prevent concurrent writes to user secrets across all instances
    private static readonly SemaphoreSlim s_fileLock = new(1, 1);
    private static string? GetUserSecretsPath()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId switch
        {
            null => Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID"),
            string id => UserSecretsPathHelper.GetSecretsPathFromSecretsId(id)
        };
    }

    public async Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken = default)
    {
        var userSecretsPath = GetUserSecretsPath();
        
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        var userSecrets = userSecretsPath is not null && File.Exists(userSecretsPath)
            ? JsonNode.Parse(await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false),
                documentOptions: jsonDocumentOptions)!.AsObject()
            : [];
        return userSecrets;
    }

    public async Task<bool> SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        // Acquire lock to prevent concurrent modifications
        await s_fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var userSecretsPath = GetUserSecretsPath();
            if (userSecretsPath is null)
            {
                throw new InvalidOperationException("User secrets path could not be determined.");
            }
            
            // Normalize to flat configuration format with colon separators
            var flattenedSecrets = FlattenJsonObject(userSecrets);
            var newSecretsJson = flattenedSecrets.ToJsonString(s_jsonSerializerOptions);
            
            // Check if user secrets have actually changed before saving
            bool hasChanges = true;
            if (File.Exists(userSecretsPath))
            {
                var existingContent = await File.ReadAllTextAsync(userSecretsPath, cancellationToken).ConfigureAwait(false);
                hasChanges = !string.Equals(existingContent.Trim(), newSecretsJson.Trim(), StringComparison.Ordinal);
            }
            
            if (hasChanges)
            {
                // Ensure directory exists before attempting to create secrets file
                Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);
                await File.WriteAllTextAsync(userSecretsPath, newSecretsJson, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Azure resource connection strings saved to user secrets.");
                return true;
            }
            
            return false;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to provision Azure resources because user secrets file is not well-formed JSON.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save user secrets.");
            return false;
        }
        finally
        {
            s_fileLock.Release();
        }
    }

    /// <summary>
    /// Flattens a JsonObject to use colon-separated keys for configuration compatibility.
    /// This ensures all secrets are stored in the flat format expected by .NET configuration.
    /// </summary>
    internal static JsonObject FlattenJsonObject(JsonObject source)
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