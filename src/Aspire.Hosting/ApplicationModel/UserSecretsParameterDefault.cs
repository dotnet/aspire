// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Wraps a <see cref="ParameterDefault"/> such that the default value is saved to the project's user secrets store.
/// </summary>
/// <param name="appHostAssembly">The app host assembly.</param>
/// <param name="applicationName">The application name.</param>
/// <param name="parameterName">The parameter name.</param>
/// <param name="parameterDefault">The <see cref="ParameterDefault"/> that will produce the default value when it isn't found in the project's user secrets store.</param>
internal sealed class UserSecretsParameterDefault(Assembly appHostAssembly, string applicationName, string parameterName, ParameterDefault parameterDefault)
    : ParameterDefault
{
    /// <inheritdoc/>
    public override string GetDefaultValue()
    {
        var value = parameterDefault.GetDefaultValue();
        var configurationKey = $"Parameters:{parameterName}";
        if (!TrySetUserSecret(appHostAssembly, configurationKey, value))
        {
            // This is a best-effort operation, so we don't throw if it fails. Common reason for failure is that the user secrets ID is not set
            // in the application's assembly. Note there's no ILogger available this early in the application lifecycle.
            Debug.WriteLine($"Failed to set value for parameter '{parameterName}' in application '{applicationName}' to user secrets.");
        }
        return value;
    }

    /// <inheritdoc/>
    public override void WriteToManifest(ManifestPublishingContext context) => parameterDefault.WriteToManifest(context);

    private static bool TrySetUserSecret(Assembly assembly, string name, string value)
    {
        if (assembly.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is { } userSecretsId)
        {
            // Save the value to the secret store
            try
            {
                var secretsStore = new SecretsStore(userSecretsId);
                secretsStore.Set(name, value);
                secretsStore.Save();
                return true;
            }
            catch (Exception) { }
        }

        return false;
    }
}
