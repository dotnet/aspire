// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IUserSecretsManager"/>.
/// </summary>
internal sealed class DefaultUserSecretsManager(ILogger<DefaultUserSecretsManager> logger) : IUserSecretsManager
{
    private static string? GetUserSecretsId()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId
               ?? Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID");
    }

    public Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken = default)
    {
        var userSecretsId = GetUserSecretsId();
        if (userSecretsId is null)
        {
            return Task.FromResult<JsonObject>([]);
        }

        try
        {
            var secretsStore = new SecretsStore(userSecretsId);
            
            var result = new JsonObject();
            foreach (var secret in secretsStore.AsEnumerable())
            {
                if (secret.Value is not null)
                {
                    result[secret.Key] = secret.Value;
                }
            }
            
            return Task.FromResult(result);
        }
        catch (Exception)
        {
            // If we can't load secrets, return empty object
            return Task.FromResult<JsonObject>([]);
        }
    }

    public Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        try
        {
            var userSecretsId = GetUserSecretsId();
            if (userSecretsId is null)
            {
                throw new InvalidOperationException("User secrets ID could not be determined.");
            }
            
            var secretsStore = new SecretsStore(userSecretsId);
            
            // Clear existing secrets before adding new ones
            secretsStore.Clear();
            
            // Set flattened secrets using SecretsStore's flattening logic
            secretsStore.SetFromJsonObject(userSecrets);
            
            // Save to file
            secretsStore.Save();

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
        
        return Task.CompletedTask;
    }
}