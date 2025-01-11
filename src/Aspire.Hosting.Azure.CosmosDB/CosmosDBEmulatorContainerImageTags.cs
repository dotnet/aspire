// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Cosmos;

internal static class CosmosDBEmulatorContainerImageTags
{
    /// <remarks>mcr.microsoft.com</remarks>
    public const string Registry = "mcr.microsoft.com";

    /// <remarks>cosmosdb/linux/azure-cosmos-emulator</remarks>
    public const string Image = "cosmosdb/linux/azure-cosmos-emulator";

    /// <remarks>latest</remarks>
    public const string Tag = "latest";
}
