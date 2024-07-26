// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Cosmos;

namespace Aspire.Hosting.Azure;

internal static class AzureCosmosDBEmulatorConnectionString
{
    public static string Create(int port) => $"AccountKey={CosmosConstants.EmulatorAccountKey};AccountEndpoint=https://127.0.0.1:{port};DisableServerCertificateValidation=True;";
}
