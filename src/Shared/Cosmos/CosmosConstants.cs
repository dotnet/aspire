// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.CosmosDB;

internal static class CosmosConstants
{
    /// <summary>
    /// Defines the application name used to interact with the Azure Cosmos database. This will be suffixed to the
    /// Cosmos user-agent to include with every Azure Cosmos database service interaction.
    /// </summary>
    internal const string CosmosApplicationName = "Aspire";

    /// <summary>
    /// Gets the well-known and documented Azure Cosmos DB emulator account key.
    /// See <a href="https://learn.microsoft.com/azure/cosmos-db/emulator#authentication"></a>
    /// </summary>
    internal const string EmulatorAccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>
    /// Gets the well-known and documented capability value used to enable serverless mode for Azure Cosmos DB accounts.
    /// </summary>
    internal const string EnableServerlessCapability = "EnableServerless";
}
