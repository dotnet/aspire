// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    public bool TryGetValue(string key, out string? value)
    {
        LogWarning(nameof(TryGetValue));
        value = null;
        return false;
    }

    public bool TrySetValue(string key, string value)
    {
        LogWarning(nameof(TrySetValue));
        return false;
    }

    public Task<bool> TrySetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        LogWarning(nameof(TrySetValueAsync));
        return Task.FromResult(false);
    }

    public string? GetOrSetValue(string key, Func<string> valueFactory)
    {
        LogWarning(nameof(GetOrSetValue));
        return null;
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
