// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.UserSecrets;

/// <summary>
/// A no-op implementation of <see cref="IUserSecretsManager"/> used when
/// user secrets are not configured for a project.
/// </summary>
internal sealed class NoopUserSecretsManager : IUserSecretsManager
{
    public static readonly NoopUserSecretsManager Instance = new();

    private NoopUserSecretsManager()
    {
    }

    public string FilePath => string.Empty;

    public bool TrySetSecret(string name, string value)
    {
        Debug.WriteLine($"User secrets are not enabled. Cannot set secret '{name}'.");
        return false;
    }

    public Task<bool> TrySetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"User secrets are not enabled. Cannot set secret '{name}'.");
        return Task.FromResult(false);
    }

    public void GetOrSetSecret(IConfigurationManager configuration, string name, Func<string> valueGenerator)
    {
        Debug.WriteLine($"User secrets are not enabled. Cannot get or set secret '{name}'.");
    }

    public Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine("User secrets are not enabled. Cannot save state.");
        return Task.CompletedTask;
    }
}
