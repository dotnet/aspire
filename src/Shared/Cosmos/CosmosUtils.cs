// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Hosting.Azure.CosmosDB;

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
        if (!builder.TryGetValue("AccountKey", out var v))
        {
            return false;
        }
        var accountKeyFromConnectionString = v.ToString();
        return accountKeyFromConnectionString == CosmosConstants.EmulatorAccountKey;
    }
}
