// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting.Publishing.Internal;

/// <summary>
/// User secrets implementation of <see cref="IDeploymentStateManager"/>.
/// </summary>
public sealed class UserSecretsDeploymentStateManager(ILogger<UserSecretsDeploymentStateManager> logger) : DeploymentStateManagerBase<UserSecretsDeploymentStateManager>(logger)
{
    /// <inheritdoc/>
    public override string? StateFilePath => GetStatePath();

    /// <inheritdoc/>
    protected override string? GetStatePath()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId switch
        {
            null => Environment.GetEnvironmentVariable("DOTNET_USER_SECRETS_ID"),
            string id => UserSecretsPathHelper.GetSecretsPathFromSecretsId(id)
        };
    }

    /// <inheritdoc/>
    protected override async Task SaveStateToStorageAsync(JsonObject state, CancellationToken cancellationToken)
    {
        try
        {
            var userSecretsPath = GetStatePath() ?? throw new InvalidOperationException("User secrets path could not be determined.");
            var flattenedUserSecrets = DeploymentStateManagerBase<UserSecretsDeploymentStateManager>.FlattenJsonObject(state);
            Directory.CreateDirectory(Path.GetDirectoryName(userSecretsPath)!);
            
            // Use the shared lock to synchronize with SecretsStore
            var lockObj = UserSecretsFileLock.GetLock(userSecretsPath);
            var json = flattenedUserSecrets.ToJsonString(s_jsonSerializerOptions);
            
            // We need to use a synchronous lock, so we'll write synchronously inside the lock
            // but still return a Task to maintain the async signature
            await Task.Run(() =>
            {
                lock (lockObj)
                {
                    File.WriteAllText(userSecretsPath, json, System.Text.Encoding.UTF8);
                }
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Azure resource connection strings saved to user secrets.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to provision Azure resources because user secrets file is not well-formed JSON.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save user secrets.");
            throw;
        }
    }
}
