// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.UserSecrets;

/// <summary>
/// A no-op implementation of <see cref="IUserSecretsManager"/> that logs warnings when
/// operations are attempted on a project without user secrets configured.
/// </summary>
internal sealed class NoopUserSecretsManager : IUserSecretsManager
{
    private readonly ILogger _logger;
    private readonly string _assemblyName;

    public NoopUserSecretsManager(ILogger logger, string assemblyName)
    {
        _logger = logger;
        _assemblyName = assemblyName;
    }

    public string FilePath => string.Empty;

    public bool TrySetSecret(string name, string value)
    {
        LogWarning(nameof(TrySetSecret));
        return false;
    }

    public Task<bool> TrySetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        LogWarning(nameof(TrySetSecretAsync));
        return Task.FromResult(false);
    }

    public void GetOrSetSecret(IConfigurationManager configuration, string name, Func<string> valueGenerator)
    {
        LogWarning(nameof(GetOrSetSecret));
    }

    public Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        LogWarning(nameof(SaveStateAsync));
        return Task.CompletedTask;
    }

    private void LogWarning(string operation)
    {
        _logger.LogWarning(
            "User secrets are not enabled for assembly '{AssemblyName}'. " +
            "Operation '{Operation}' will not persist data. " +
            "To enable user secrets, add a UserSecretsId to your project file or use 'dotnet user-secrets init'.",
            _assemblyName,
            operation);
    }
}
