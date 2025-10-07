// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Storage;

namespace Aspire.Hosting.Azure.Utils;

internal static class StorageAccountHelpers
{
    /// <summary>
    /// Applies common best practice settings to a storage account to ensure security and compliance.
    /// </summary>
    /// <param name="storageAccount">The storage account to configure.</param>
    internal static void ApplyBestPracticeDefaults(StorageAccount storageAccount)
    {
        // Set the storage account kind to StorageV2 to enable the latest features
        storageAccount.Kind = StorageKind.StorageV2;

        // Set the minimum TLS version to 1.2 to ensure resources provisioned are compliant
        // with the pending deprecation of TLS 1.0 and 1.1.
        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2;
    }
}
