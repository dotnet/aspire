// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Hosting.Azure.Cosmos;

internal static class CosmosUtils
{
    internal static bool IsEmulatorConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return false;
        }

        var builder = new DbConnectionStringBuilder();
        builder.ConnectionString = connectionString;
        var accountKeyFromConnectionString = builder["AccountKey"].ToString();
        return accountKeyFromConnectionString == CosmosConstants.EmulatorAccountKey;
    }
}
