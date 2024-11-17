// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Storage;

internal static class StorageEmulatorContainerImageTags
{
    /// <summary>mcr.microsoft.com</summary>
    public const string Registry = "mcr.microsoft.com";

    /// <summary>azure-storage/azurite</summary>
    public const string Image = "azure-storage/azurite";

    /// <summary>3.33.0</summary>
    public const string Tag = "3.33.0";
}
