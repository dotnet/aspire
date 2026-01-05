// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREUSERSECRETS001

using System.Diagnostics;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Wraps a <see cref="ParameterDefault"/> such that the default value is saved to the project's user secrets store.
/// </summary>
/// <param name="applicationName">The application name.</param>
/// <param name="parameterName">The parameter name.</param>
/// <param name="parameterDefault">The <see cref="ParameterDefault"/> that will produce the default value when it isn't found in the project's user secrets store.</param>
/// <param name="userSecretsManager">The user secrets manager.</param>
internal sealed class UserSecretsParameterDefault(string applicationName, string parameterName, ParameterDefault parameterDefault, IUserSecretsManager userSecretsManager)
    : ParameterDefault
{
    /// <inheritdoc/>
    public override string GetDefaultValue()
    {
        var configurationKey = $"Parameters:{parameterName}";

        return userSecretsManager.GetOrSetSecret(configurationKey, parameterDefault.GetDefaultValue);
    }

    /// <inheritdoc/>
    public override void WriteToManifest(ManifestPublishingContext context) => parameterDefault.WriteToManifest(context);
}
