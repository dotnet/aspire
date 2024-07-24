// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Utils;

internal static class ILocalStorageExtensions
{
    /// <summary>
    /// Retrieves the value associated with the specified key.
    /// If there is a CryptographicException, return default instead of throwing. A CryptographicException can occur
    /// because the local data protection key for the dashboard has changed, and previously stored data can no longer be
    /// successfully decrypted.
    /// </summary>
    public static async Task<LocalStorageResult<T>> SafeGetAsync<T>(this ILocalStorage value, string key)
    {
        try
        {
            return await value.GetAsync<T>(key).ConfigureAwait(false);
        }
        catch (CryptographicException ex)
        {
            Debug.WriteLine($"Failed to decrypt data for key '{key}': {ex.Message}");
            return default;
        }
    }
}
