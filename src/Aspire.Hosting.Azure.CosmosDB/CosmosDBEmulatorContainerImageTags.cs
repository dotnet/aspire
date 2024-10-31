// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Cosmos;

internal static class CosmosDBEmulatorContainerImageTags
{
    /// <summary>mcr.microsoft.com</summary>
    public const string Registry = "mcr.microsoft.com";

    /// <summary>cosmosdb/linux/azure-cosmos-emulator</summary>
    public const string Image = "cosmosdb/linux/azure-cosmos-emulator";

    /// <summary>latest</summary>
    public const string Tag = "latest";
}
