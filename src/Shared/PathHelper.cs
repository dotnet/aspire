// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// Helper class for path operations related to user secrets.
/// This delegates to UserSecretsPathHelper to maintain consistency.
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// Returns the path to the JSON file that stores user secrets.
    /// </summary>
    /// <param name="userSecretsId">The user secret ID.</param>
    /// <returns>The full path to the secret file.</returns>
    public static string GetSecretsPathFromSecretsId(string userSecretsId)
    {
        return UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
    }
}