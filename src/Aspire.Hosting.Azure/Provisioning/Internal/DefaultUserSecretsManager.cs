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

    public async Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        try
        {
            var userSecretsPath = GetUserSecretsPath();
            if (userSecretsPath is null)
            {
                throw new InvalidOperationException("User secrets path could not be determined.");
            }
            
            // Normalize to flat configuration format with colon separators
            var flattenedSecrets = FlattenJsonObject(userSecrets);
            
            // Ensure directory exists before attempting to create secrets file
            Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);
            await File.WriteAllTextAsync(userSecretsPath, flattenedSecrets.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);

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
            else
            {
                result[key] = kvp.Value?.DeepClone();
            }
        }
    }
}