// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.UserSecrets;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Publishing.Internal;

/// <summary>
/// User secrets implementation of <see cref="IDeploymentStateManager"/>.
/// </summary>
internal sealed class UserSecretsDeploymentStateManager : DeploymentStateManagerBase<UserSecretsDeploymentStateManager>
{
    private readonly IUserSecretsManager _userSecretsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsDeploymentStateManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userSecretsManager">User secrets manager for managing secrets.</param>
    public UserSecretsDeploymentStateManager(ILogger<UserSecretsDeploymentStateManager> logger, IUserSecretsManager userSecretsManager) 
        : base(logger)
    {
        _userSecretsManager = userSecretsManager;
    }

    /// <inheritdoc/>
    public override string? StateFilePath => _userSecretsManager.FilePath;

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
            // Use the shared manager which handles locking
            await _userSecretsManager.SaveStateAsync(state, cancellationToken).ConfigureAwait(false);
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
