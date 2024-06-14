// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Wraps a <see cref="ParameterDefault"/> such that the default value is saved to the project's user secrets store.
/// </summary>
/// <param name="applicationName">The application name.</param>
/// <param name="parameterName">The parameter name.</param>
/// <param name="parameterDefault">The parameter with default value.</param>
public sealed class UserSecretsParameterDefault(string applicationName, string parameterName, ParameterDefault parameterDefault)
    : ParameterDefault
{
    /// <inheritdoc/>
    public override string GetDefaultValue()
    {
        var value = parameterDefault.GetDefaultValue();
        var configurationKey = $"{ParameterResourceBuilderExtensions.ConfigurationSectionKey}:{parameterName}";
        if (!TrySetUserSecret(applicationName, configurationKey, value))
        {
            throw new DistributedApplicationException($"Failed to set value for parameter '{parameterName}' in application '{applicationName}' to user secrets.");
        }
        return value;
    }

    /// <inheritdoc/>
    public override void WriteToManifest(ManifestPublishingContext context) => parameterDefault.WriteToManifest(context);

    private static bool TrySetUserSecret(string applicationName, string name, string value)
    {
        if (!string.IsNullOrEmpty(applicationName))
        {
            var appAssembly = Assembly.Load(new AssemblyName(applicationName));
            if (appAssembly is not null && appAssembly.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is { } userSecretsId)
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
        }

        return false;
    }
}
